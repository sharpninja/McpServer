# FR-SUPPORT

[Back to Functional Requirements](Functional-Requirements)

## FR-SUPPORT-010 MCP Context Unification

Local MCP server providing context retrieval, TODO management, repository access, session logging, and ingestion capabilities for AI agent integration.

**Covered by:** `ContextController`, `TodoController`, `RepoController`, `SessionLogController`, `QBrainAiMcpTools`, `McpDbContext`, `HybridSearchService`, `EmbeddingService`, `VectorIndexService`, `Fts5SearchService`, `RepoFileService`, `IngestionCoordinator`
Scope: layer-1+

## FR-SUPPORT-010A SessionLog Workspace Stamping

Session log POST shall stamp the resolved workspace ID on every persisted row (parent SessionLog plus all child entities: turns, actions, tags, context items, processing dialog, commits, string-list items) so a POST followed by a GET under the same workspace context returns the same record. When no workspace context is resolved (ingestion / batch import paths), WorkspaceId defaults to empty string and the DbContext-level auto-stamp populates it from `_workspaceId` if available.

**Covered by:** `SessionLogService.StampWorkspaceId`, `McpDbContext.StampWorkspaceId`, `SessionLogControllerTests.WhenPostingThenGetBySessionIdReturnsRecord`
Scope: layer-1+

## FR-SUPPORT-010B SessionLog ProblemDetails Errors

Session log POST shall return RFC 7807 ProblemDetails on body-binding or validation failure. Error responses cite the offending JSON path under `errors`, never the action-parameter name. Content-Type is `application/problem+json`. The accepted top-level shape is documented in the response `detail`.

**Covered by:** `Program.cs` (`InvalidModelStateResponseFactory`), `SessionLogController.SubmitAsync` (`ValidationProblem` calls), `SessionLogControllerTests.WhenPostingMalformedWorkspaceFieldThenReturnsProblemDetailsWithoutDtoKey`
Scope: layer-1+

## FR-SUPPORT-010C SessionLog REST Surface Completion

Session log REST shall expose `GET /qbrainai/sessionlog/{agent}/{sessionId}` (single-record fetch under tenancy) and `POST /qbrainai/sessionlog/{agent}/{sessionId}/turn` (turn-append by RequestId). Unsupported verbs on either route return 405 Method Not Allowed with an `Allow` header.

**Covered by:** `SessionLogController.GetByIdAsync`, `SessionLogController.UpsertTurnAsync`, `SessionLogService.GetAsync`, `SessionLogService.UpsertTurnAsync`
Scope: layer-1+

## FR-SUPPORT-010E Stateless session lifecycle endpoints

The session-log API SHALL expose stateless open/begin/complete/fail lifecycle operations keyed by (agent, sessionId, requestId) requiring no in-process active-session state.
Scope: layer-1+

## FR-SUPPORT-010F Additive partial session-log submits

Whole-session submit SHALL merge additively: omitted session and turn fields never overwrite previously persisted values.
Scope: layer-1+

## FR-SUPPORT-011 SessionLog Workspace Stamping

Session log POST shall stamp the resolved workspace ID on every persisted row (parent SessionLog plus all child entities: turns, actions, tags, context items, processing dialog, commits, string-list items) so a POST followed by a GET under the same workspace context returns the same record. When no workspace context is resolved (ingestion / batch import paths), WorkspaceId defaults to empty string and the DbContext-level auto-stamp populates it from _workspaceId if available.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] Session log POST stamps resolved workspace ID on parent and all child entities
- [ ] POST followed by GET under same workspace returns same record with workspace ID preserved
- [ ] When no workspace context is resolved, WorkspaceId defaults to empty string and DbContext auto-stamp populates from _workspaceId if available

## FR-SUPPORT-012 SessionLog ProblemDetails Errors

Session log POST shall return RFC 7807 ProblemDetails on body-binding or validation failure. Error responses cite the offending JSON path under errors, never the action-parameter name. Content-Type is application/problem+json. The accepted top-level shape is documented in the response detail.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] Session log POST returns RFC 7807 ProblemDetails on body-binding failure
- [ ] Session log POST returns RFC 7807 ProblemDetails on validation failure
- [ ] Error responses cite offending JSON path under errors, not action-parameter name
- [ ] Content-Type is application/problem+json and accepted top-level shape is documented in response detail

## FR-SUPPORT-013 SessionLog REST Surface Completion

Session log REST shall expose GET /qbrainai/sessionlog/{agent}/{sessionId} (single-record fetch under tenancy) and POST /qbrainai/sessionlog/{agent}/{sessionId}/turn (turn-append by RequestId). Unsupported verbs return 405 Method Not Allowed with Allow header. Terminal-turn audit-evidence gate enforced only for Quad-Brain ACID hosted-agent source type (QBAgent), not standard agents.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] GET /mcpserver/sessionlog/{agent}/{sessionId} returns single-record fetch under tenancy
- [ ] POST /mcpserver/sessionlog/{agent}/{sessionId}/turn appends turn by RequestId
- [ ] Unsupported verbs return 405 Method Not Allowed with Allow header
- [ ] Terminal-turn audit-evidence gate enforced only for QBAgent (ACID hosted-agent), not standard agents

## FR-SUPPORT-014 Stateless session lifecycle endpoints

The session-log API shall expose stateless open/begin/complete/fail lifecycle operations keyed by (agent, sessionId, requestId) requiring no in-process active-session state.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] Session-log API exposes stateless open/begin/complete/fail lifecycle operations
- [ ] Lifecycle operations are keyed by (agent, sessionId, requestId) tuple
- [ ] Operations require no in-process active-session state to function

## FR-SUPPORT-015 Additive partial session-log submits

Whole-session submit shall merge additively: omitted session and turn fields never overwrite previously persisted values. This ensures partial updates preserve existing state.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] Whole-session submit merges additively without overwriting omitted fields
- [ ] Omitted session fields are never overwritten during additive merge
- [ ] Omitted turn fields are never overwritten during additive merge

## FR-SUPPORT-016 FR-SUPPORT-016

Placeholder requirement backfilled for TODO link FR-SUPPORT-016.
Scope: layer-1+

<!-- mcp-wiki-footer:begin -->
This wiki was generated by QBrain. [Explore the QBrain project on GitHub](https://github.com/sharpninja/McpServer).
<!-- mcp-wiki-footer:end -->
