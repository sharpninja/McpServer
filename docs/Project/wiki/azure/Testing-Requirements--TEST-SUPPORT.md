# TEST-SUPPORT

[Back to Testing Requirements](Testing-Requirements)

## TEST-SUPPORT

### TEST-SUPPORT-010

Traceability audit coverage for the original broad FR-SUPPORT-010 support surface row. This broad parent row predates the later FR-SUPPORT-010A through FR-SUPPORT-010F split and maps to this explicit audit TEST ID while split child rows retain their dedicated support test IDs.


### TEST-SUPPORT-010A-1

Given a `SessionLogService` constructed with a non-null `WorkspaceContext`, when `SubmitAsync` persists a session, then `SessionLogEntity.WorkspaceId` and every child entity's `WorkspaceId` equal the context's `WorkspacePath`. **Covered by:** `SessionLogServiceTests.SubmitAsync_StampsWorkspaceIdOnSessionEntity`, `SessionLogServiceTests.SubmitAsync_StampsWorkspaceIdOnEveryChildEntity`


### TEST-SUPPORT-010A-2

Given a `SessionLogService` constructed with `workspaceContext: null` and a DbContext without `_workspaceId`, when `SubmitAsync` persists a session, then `SessionLogEntity.WorkspaceId` remains empty string. **Covered by:** `SessionLogServiceTests.SubmitAsync_WithNullWorkspaceContext_KeepsWorkspaceIdEmpty`


### TEST-SUPPORT-010B-1

Given a malformed POST body that fails JSON deserialization against `UnifiedSessionLogDto`, when the controller returns 400, then the response content-type is `application/problem+json` and the errors object contains `$.workspace` (or the offending field path), never the `dto` parameter name. **Covered by:** `SessionLogControllerTests.WhenPostingMalformedWorkspaceFieldThenReturnsProblemDetailsWithoutDtoKey`


### TEST-SUPPORT-010B-2

Given a POST body missing `sourceType`, when domain validation rejects it, then the response is `application/problem+json` with `sourceType` cited (not the legacy `{"error":"..."}` plain shape). **Covered by:** `SessionLogControllerTests.WhenPostingMissingSourceTypeThenReturnsProblemDetails`


### TEST-SUPPORT-010C-1

Given a successful POST to `/mcpserver/sessionlog`, when `GET /mcpserver/sessionlog/{agent}/{sessionId}` is called under the same workspace context, then the response is 200 OK with the round-tripped session. **Covered by:** `SessionLogControllerTests.WhenPostingThenGetBySessionIdReturnsRecord`


### TEST-SUPPORT-010C-2

Given a session exists, when `POST /mcpserver/sessionlog/{agent}/{sessionId}/turn` carries a `UnifiedRequestEntryDto`, then 201 is returned and the subsequent GET shows the appended turn. **Covered by:** `SessionLogControllerTests.WhenPostingTurnViaRestThenTurnIsRetrievable`, `SessionLogServiceTests.UpsertTurnAsync_NewTurn_AppendsWithoutDeletingSiblings`


### TEST-SUPPORT-010C-3

Given the turn-append route, when PUT is used instead of POST, then 405 is returned with `Allow: POST`. **Covered by:** `SessionLogControllerTests.WhenPuttingTurnRouteThenReturns405WithAllowHeader`


### TEST-SUPPORT-010E

Integration tests: open idempotent, begin creates in_progress, complete merges+finalizes with evidence gate, fail records note, missing session 404.


### TEST-SUPPORT-010F

SQLite tests: partial session submit preserves omitted title/model; sparse turn submit preserves omitted response/queryText and prior collections.


### TEST-SUPPORT-014

Integration tests verify session log operations: open idempotent, begin creates in_progress, complete merges and finalizes with evidence gate, fail records note, missing session returns 404.

**Acceptance Criteria:**
- [x] Open operation is idempotent
- [x] Begin creates session in in_progress state
- [x] Complete merges and finalizes with evidence gate
- [x] Fail records note in session

### TEST-SUPPORT-015

SQLite tests verify that partial session submit preserves omitted title/model and sparse turn submit preserves omitted response/queryText and prior collections.

**Acceptance Criteria:**
- [x] Partial session submit preserves omitted title and model fields
- [x] Sparse turn submit preserves omitted response and queryText fields
- [x] Prior collections are preserved during partial updates

### TEST-SUPPORT-016

When SessionLogService with a non-null WorkspaceContext calls SubmitAsync, SessionLogEntity.WorkspaceId and every child entity's WorkspaceId equal the context's WorkspacePath. This ensures proper workspace isolation for session logs.

**Acceptance Criteria:**
- [x] SessionLogEntity.WorkspaceId matches context WorkspacePath
- [x] Every child entity's WorkspaceId matches context WorkspacePath

### TEST-SUPPORT-017

When SessionLogService is constructed with workspaceContext: null and a DbContext without _workspaceId, SubmitAsync persists a session with SessionLogEntity.WorkspaceId remaining empty string.

**Acceptance Criteria:**
- [x] WorkspaceId remains empty string when workspace context is null
- [x] Session persists successfully without workspace context

### TEST-SUPPORT-018

When a malformed POST body fails JSON deserialization against UnifiedSessionLogDto, the controller returns 400 with application/problem+json content-type and errors object contains the field path (e.g., $.workspace), never the dto parameter name.

**Acceptance Criteria:**
- [x] Response content-type is application/problem+json
- [x] Errors object contains field path ($.workspace) not parameter name

### TEST-SUPPORT-019

When a POST body is missing sourceType, domain validation rejects it with application/problem+json response citing sourceType (not legacy {"error":"..."} plain shape).

**Acceptance Criteria:**
- [x] Response is application/problem+json format
- [x] sourceType field is cited in error response

### TEST-SUPPORT-020

When a successful POST to /mcpserver/sessionlog completes, GET /mcpserver/sessionlog/{agent}/{sessionId} under the same workspace context returns 200 OK with the round-tripped session.

**Acceptance Criteria:**
- [x] POST to /mcpserver/sessionlog succeeds
- [x] GET /mcpserver/sessionlog/{agent}/{sessionId} returns 200 OK
- [x] Response contains the round-tripped session data

### TEST-SUPPORT-021

When the turn-append route receives a POST to /mcpserver/sessionlog/{agent}/{sessionId}/turn with a UnifiedRequestEntryDto, 201 is returned and the subsequent GET shows the appended turn.

**Acceptance Criteria:**
- [x] POST to turn route returns 201 Created
- [x] Subsequent GET shows the appended turn

### TEST-SUPPORT-022

When PUT is used on the turn-append route instead of POST, the endpoint returns 405 Method Not Allowed with 'Allow: POST' header.

**Acceptance Criteria:**
- [x] PUT request returns 405 Method Not Allowed
- [x] Response includes Allow: POST header

### TEST-SUPPORT-024

Validates FR-SUPPORT-016 ac-1 to ac-4 and ac-6 to ac-8 and TR-SUPPORT-CORE-016 ac-1: SessionLogServiceReplaceDeleteTests for the existing behavior, and the PLAN-SESSIONTRIAGEFIX-001 A7, A8 and A9 tests (SessionLogServiceTests, SessionLogClientRequestBodyTests, SessionLogControllerDeleteItemTests, and SessionLogDeleteItemRoutingTests on a temporary instance).

**Acceptance Criteria:**
- [ ] FR-SUPPORT-016 ac-1 to ac-4 and TR-SUPPORT-CORE-016 ac-1: SessionLogServiceReplaceDeleteTests ReplaceTurnAsync_ClearsOmittedScalarsAndCollections, ReplaceTurnAsync_PreservesWorkspaceStampOnChildren, ReplaceTurnSectionAsync_ReplacesOnlyNamedSection, ReplaceTurnSectionAsync_EmptyPayload_ClearsSection, ClearTurnSectionAsync_RemovesAllItemsInSection, UpsertTurnAsync_StaysAdditive_DoesNotRemoveExistingItems, DeleteTurnItemAsync_StringSection_RemovesByValue, DeleteTurnItemAsync_Commit_RemovesBySha, DeleteTurnItemAsync_Action_RemovesByOrder, DeleteTurnItemAsync_UnknownKey_ReturnsFalse and ReplaceTurnSectionAsync_UnknownSection_Throws pass.
- [ ] FR-SUPPORT-016 ac-6 (PLAN-SESSIONTRIAGEFIX-001 A7): the ordinal mock gate and the three ordinal and delete cases named there pass.
- [ ] FR-SUPPORT-016 ac-7 (PLAN-SESSIONTRIAGEFIX-001 A8): the client mock gate and request case, the two controller cases and the two routing cases on a temporary instance named there pass.
- [ ] FR-SUPPORT-016 ac-8 (PLAN-SESSIONTRIAGEFIX-001 A9): the null-element mock gate and the null-list, null-submit and empty-list cases named there pass.
- [ ] FR-SUPPORT-016 ac-6 for concurrent writes and TR-SUPPORT-CORE-016 ac-4: the mock gate ConcurrentTurnWriteOutcomeAssertion_AcceptsDistinctOrRetryableRefusalAndRejectsDuplicateOrPartialWrite (QBrainAi.Support.Mcp.Tests; it also rejects two first inserts stored as two turn rows), then TurnWrites_InterleavedOnOneTurn_StoreDistinctOrdinalsOrRefuseRetryably (a file SQLite database; the second writer runs between the first writer's read and save, for a PATCH, a dialog append and a section replace), TurnWrites_TwoFirstInsertsOfOneRequestId_LeaveOneTurnRow (two PATCHes that each create the same new turn, interleaved the same way: one turn row remains, holding both writes' items with distinct ordinals) and SessionLogConcurrentTurnWriteIntegrationTests (concurrent dialog appends to one turn on SQLite, PostgreSQL and SQL Server temporary instances, every refusal retryable and retried, every stored ordinal distinct) pass; for ac-4's retry, the mock gate UniqueRaceAssertion_AcceptsOneRetriedUpdateAndRejectsMissingRetryOrDuplicate (SessionLogServiceTests), then UpsertTurnAsync_UniqueSessionLogTurnsRace_RetriesOnceAndUpdates, which forces the unique violation inside the service's own transaction and commits the winning row after the rollback, passes: the retry runs once, in a fresh transaction, and updates that row, leaving one turn row (no caller reaches this path in production today, because UpsertTurnAsync serializes its read and write whenever no transaction is open and no caller holds one) (PLAN-SESSIONTRIAGEFIX-001 A7 follow-up; operator decision 2026-10-16 "Keep retry, revert ac-1 (Recommended)").

<!-- mcp-wiki-footer:begin -->
This wiki was generated by QBrain. [Explore the QBrain project on GitHub](https://github.com/sharpninja/McpServer).
<!-- mcp-wiki-footer:end -->
