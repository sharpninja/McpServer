# Session Log Workflow API Reference

This document describes the iteration 2 public interfaces for Session Log workflow operations in `McpServer.Repl.Core`.

## Overview

The Session Log workflow API provides structured operations for agent-driven audit trails, including:

- Session lifecycle management (bootstrap, open, query)
- Turn lifecycle management (begin, update, complete, fail)
- Dialog streaming (reasoning, tool calls, observations, decisions)
- Action tracking (file edits, design decisions, commits, etc.)
- Active session/turn state tracking

## Core Interfaces

### ISessionLogWorkflow

Primary workflow interface with 10 operations:

1. **BootstrapAsync** — Initialize session log subsystem (idempotent)
2. **OpenSessionAsync** — Create new session with metadata
3. **CurrentSession** — Retrieve active session state
4. **BeginTurnAsync** — Start new turn within active session
5. **UpdateTurnAsync** — Modify active turn metadata
6. **CompleteTurnAsync** — Finalize turn as completed (immutable)
7. **FailTurnAsync** — Finalize turn as failed (immutable)
8. **AppendDialogAsync** — Add dialog items to active turn
9. **AppendActionsAsync** — Add actions to active turn
10. **QueryHistoryAsync** — Query session log history with filtering/pagination

### ISessionLogState

Runtime state tracking for the active session and turn:

- Session metadata (agent, sessionId, title, model, timestamps, status)
- Active turn tracking (currentTurnRequestId, currentTurnStatus)
- Session statistics (turnCount)

### Supporting Interfaces

- **IDialogItem** — Dialog entry with timestamp, role, content, category
- **ISessionAction** — Action entry with order, description, type, status, filePath
- **ISessionLogSummary** — Session summary for query results

## Canonical Identifier Rules

### Agent Name
- Format: PascalCase (e.g., "Copilot", "Cline", "Cursor")
- Must match the sourceType prefix in sessionId

### Session ID
- Format: `<Agent>-<yyyyMMddTHHmmssZ>-<suffix>`
- Regex: `^[A-Z][A-Za-z0-9]*-\d{8}T\d{6}Z-[a-z0-9]+(?:-[a-z0-9]+)*$`
- Valid: `Copilot-20260304T113901Z-namingconv`
- Invalid: `copilot-20260304T113901Z-namingconv` (lowercase prefix)
- Invalid: `Copilot-2026-03-04-namingconv` (wrong date format)

### Request ID
- Format: `req-<yyyyMMddTHHmmssZ>-<slugOrOrdinal>`
- Regex: `^req-\d{8}T\d{6}Z-[a-z0-9]+(?:-[a-z0-9]+)*$`
- Valid: `req-20260304T113901Z-plan-namingconventions-001`
- Invalid: `req-plan-namingconventions-001` (missing timestamp)
- Invalid: `request-20260304T113901Z-task-01` (wrong prefix)

## Turn Lifecycle State Transitions

1. **Created (in_progress)**
   - Turn initiated via `BeginTurnAsync`
   - Initial state allows dialog and action appends
   - Mutable: can be updated via `UpdateTurnAsync`, `AppendDialogAsync`, `AppendActionsAsync`

2. **Updated (in_progress)**
   - Turn modified via `UpdateTurnAsync`, `AppendDialogAsync`, or `AppendActionsAsync`
   - Remains mutable until completed or failed

3. **Completed (completed)**
   - Turn finalized via `CompleteTurnAsync`
   - Immutable: no further modifications allowed
   - Terminal state

4. **Failed (failed)**
   - Turn marked as failed via `FailTurnAsync`
   - Immutable: no further modifications allowed
   - Terminal state
   - Captures error message and optional error code

## Replacing and Removing Data (PATCH / PUT / DELETE)

The normal turn lifecycle is **append-only and additive**: `POST`/`PATCH` and the
`begin`/`complete`/`fail` verbs merge their payload onto the existing turn, so an
omitted field never clobbers a previously recorded value and collection items are
appended. That is the right default for an audit trail, but it means a plain
submit can never *remove* a section of data. The verb split below makes removal
explicit. Intent is carried by the **HTTP verb**, not by sending nulls, so there
is no ambiguity between "field absent" and "clear this field".

| Verb | Scope | Semantics |
|------|-------|-----------|
| `POST` / `PATCH` | turn | **Additive merge.** Omitted scalars preserved; collection items appended. (PATCH is the explicit alias for the long-standing additive submit.) |
| `PUT` | turn | **Replace.** Omitted scalars reset; every section becomes exactly the payload; omitted/empty sections cleared. |
| `PUT` | section | **Replace one section.** Only the named section is rewritten from the payload; an empty/omitted property clears it. Other sections untouched. |
| `DELETE` | section | Clear all items in one section. |
| `DELETE` | section item | Remove a single item from a section. |
| `DELETE` | turn | Soft-delete a turn and all its child rows (the session is preserved). |
| `DELETE` | session | Soft-delete a session and every turn beneath it. Canonical session ids only; imported provider-native ids are rejected. |

`PUT` and `DELETE` are **correction operations**: unlike the append-only
lifecycle, they intentionally rewrite or hide recorded data, including on
turns already marked `completed`/`failed`. Use them to fix a mis-logged turn,
not as part of normal turn flow. The terminal-turn compliance gate
(at least one decision/action/commit) still applies to a `PUT` that sets a
terminal status, but not to section/item/whole-turn `DELETE`.

Every `DELETE` is a **soft delete**: rows are tombstoned with deletion
metadata, never physically removed, and the tombstoned session still holds the
unique `(workspace, sourceType, sessionId)` key. Resubmitting a session with
the same key (for example re-running a transcript import) revives the
tombstoned session graph and applies the resubmitted data. To repair wrong
turn content, resubmit the turns with correct data or use the turn-level
operations; do not route repairs through session delete.

Imported transcript sessions carry provider-native identifiers (UUID session
ids, tool-call request ids). The turn-level operations (`PUT`/`DELETE` on
turns, sections, and items) accept those identifiers so imported turns stay
repairable; session `DELETE` validates the canonical id format and therefore
never targets an imported session.

### Sections

`actions`, `tags`, `context`, `dialog`, `commits`, `designDecisions`,
`requirementsDiscovered`, `filesModified`, `blockers`.

### Item keys (for single-item DELETE)

- String sections (`tags`, `context`, `designDecisions`, `requirementsDiscovered`, `filesModified`, `blockers`): the item **value**.
- `commits`: the commit **SHA**.
- `actions`: the action **Order**.
- `dialog`: the item **ordinal**.

### REST endpoints

```
PATCH  /mcpserver/sessionlog/{agent}/{sessionId}/{requestId}                       # additive merge
PUT    /mcpserver/sessionlog/{agent}/{sessionId}/{requestId}                       # replace whole turn
PUT    /mcpserver/sessionlog/{agent}/{sessionId}/{requestId}/sections/{section}    # replace one section
DELETE /mcpserver/sessionlog/{agent}/{sessionId}/{requestId}/sections/{section}/items/{itemKey}
DELETE /mcpserver/sessionlog/{agent}/{sessionId}/{requestId}/sections/{section}    # clear section
DELETE /mcpserver/sessionlog/{agent}/{sessionId}/{requestId}                       # delete turn
DELETE /mcpserver/sessionlog/{agent}/{sessionId}                                   # delete session
```

The replace/section bodies are a `UnifiedRequestEntryDto`; for a section PUT only
the matching property is read. `DELETE` returns `404` when the target does not
exist and is idempotent on retry (re-deleting an already-removed item/turn/section
is safe).

### MCP tools

- `sessionlog_replace_turn` (turn replace)
- `sessionlog_replace_section` (section replace)
- `sessionlog_clear_section` (clear a section)
- `sessionlog_delete_item` (remove one item)
- `sessionlog_delete_turn` (delete a turn)
- `sessionlog_delete_session` (delete a session)

### REPL passthrough

The typed client methods are exposed verbatim over `client.SessionLog.*`:
`ReplaceTurn`, `ReplaceTurnSection`, `ClearTurnSection`, `DeleteTurnItem`,
`DeleteTurn`, `DeleteSession`.

### Removal recipes

- **Drop one tag**: `DELETE .../sections/tags/items/<tag-value>`
- **Clear all blockers** (e.g. after they are resolved): `DELETE .../sections/blockers`
- **Replace the file list** with a corrected set: `PUT .../sections/filesModified` with `{ "filesModified": ["src/a.cs"] }`
- **Strip a mis-recorded commit**: `DELETE .../sections/commits/items/<sha>`
- **Rewrite a whole turn** (omit a section to clear it): `PUT .../{requestId}` with the corrected `UnifiedRequestEntryDto`
- **Remove an accidental turn**: `DELETE .../{requestId}`

## YAML Command Shapes

All commands use the `workflow.sessionlog.*` namespace.

### Command Methods

- `workflow.sessionlog.bootstrap`
- `workflow.sessionlog.openSession`
- `workflow.sessionlog.currentSession`
- `workflow.sessionlog.beginTurn`
- `workflow.sessionlog.updateTurn`
- `workflow.sessionlog.completeTurn`
- `workflow.sessionlog.failTurn`
- `workflow.sessionlog.appendDialog`
- `workflow.sessionlog.appendActions`
- `workflow.sessionlog.queryHistory`

### Request Envelope Structure

```yaml
type: request
payload:
  requestId: <unique-request-id>
  method: workflow.sessionlog.<operation>
  params:
    <operation-specific-parameters>
```

### Result Envelope Structure

```yaml
type: result
payload:
  requestId: <matching-request-id>
  result:
    <operation-specific-result>
```

### Error Envelope Structure

```yaml
type: error
payload:
  requestId: <matching-request-id>
  code: <error-code>
  message: <human-readable-message>
  details:
    <optional-context-specific-details>
```

## Error Codes

Standard error codes for session log operations:

- **bootstrap_failed** — Bootstrap operation failed
- **session_not_found** — No active session exists
- **session_already_exists** — Session with same ID already exists
- **invalid_session_id** — Session ID violates canonical identifier rules
- **invalid_request_id** — Request ID violates canonical identifier rules
- **turn_not_found** — No active turn exists
- **turn_already_exists** — Turn with same request ID already exists
- **turn_immutable** — Turn is completed or failed and cannot be modified
- **invalid_turn_state** — Operation not allowed in current turn state
- **invalid_parameter** — Required parameter missing or invalid
- **storage_error** — Underlying storage operation failed
- **internal_error** — Unexpected internal error

## Example Workflow

```yaml
# 1. Bootstrap
type: request
payload:
  requestId: req-20260304T113901Z-bootstrap-001
  method: workflow.sessionlog.bootstrap
  params: {}
---
type: result
payload:
  requestId: req-20260304T113901Z-bootstrap-001
  result:
    initialized: true

# 2. Open Session
---
type: request
payload:
  requestId: req-20260304T113901Z-open-001
  method: workflow.sessionlog.openSession
  params:
    agent: Copilot
    sessionId: Copilot-20260304T113901Z-feature-auth
    title: Implementing JWT authentication
    model: claude-sonnet-4-20250514
---
type: result
payload:
  requestId: req-20260304T113901Z-open-001
  result:
    sessionId: Copilot-20260304T113901Z-feature-auth
    started: 2026-03-04T11:39:01Z

# 3. Begin Turn
---
type: request
payload:
  requestId: req-20260304T113901Z-beginturn-001
  method: workflow.sessionlog.beginTurn
  params:
    requestId: req-20260304T113901Z-add-jwt-001
    queryTitle: Add JWT authentication
    queryText: Implement JWT token generation and validation for the API
---
type: result
payload:
  requestId: req-20260304T113901Z-beginturn-001
  result:
    requestId: req-20260304T113901Z-add-jwt-001
    timestamp: 2026-03-04T11:45:23Z
    status: in_progress

# 4. Append Dialog
---
type: request
payload:
  requestId: req-20260304T113901Z-appenddialog-001
  method: workflow.sessionlog.appendDialog
  params:
    dialogItems:
      - timestamp: 2026-03-04T11:46:00Z
        role: model
        content: Analyzing authentication requirements...
        category: reasoning
---
type: result
payload:
  requestId: req-20260304T113901Z-appenddialog-001
  result:
    appended: 1
    totalDialogItems: 1

# 5. Append Actions
---
type: request
payload:
  requestId: req-20260304T113901Z-appendactions-001
  method: workflow.sessionlog.appendActions
  params:
    actions:
      - order: 1
        description: Created TokenService class
        type: create
        status: completed
        filePath: src/TokenService.cs
---
type: result
payload:
  requestId: req-20260304T113901Z-appendactions-001
  result:
    appended: 1
    totalActions: 1

# 6. Update Turn
---
type: request
payload:
  requestId: req-20260304T113901Z-update-001
  method: workflow.sessionlog.updateTurn
  params:
    response: Created TokenService and JwtValidator classes
    interpretation: User wants JWT authentication with token generation and validation
    tokenCount: 1250
    tags:
      - feature
      - security
    contextList:
      - src/TokenService.cs
      - src/JwtValidator.cs
---
type: result
payload:
  requestId: req-20260304T113901Z-update-001
  result:
    updated: true
    lastUpdated: 2026-03-04T11:46:15Z

# 7. Complete Turn
---
type: request
payload:
  requestId: req-20260304T113901Z-complete-001
  method: workflow.sessionlog.completeTurn
  params:
    response: JWT authentication successfully implemented with token generation and validation
---
type: result
payload:
  requestId: req-20260304T113901Z-complete-001
  result:
    requestId: req-20260304T113901Z-add-jwt-001
    status: completed
    completedAt: 2026-03-04T11:50:00Z

# 8. Query History
---
type: request
payload:
  requestId: req-20260304T113901Z-query-001
  method: workflow.sessionlog.queryHistory
  params:
    agent: Copilot
    limit: 10
    offset: 0
---
type: result
payload:
  requestId: req-20260304T113901Z-query-001
  result:
    sessions:
      - agent: Copilot
        sessionId: Copilot-20260304T113901Z-feature-auth
        title: Implementing JWT authentication
        model: claude-sonnet-4-20250514
        started: 2026-03-04T11:39:01Z
        lastUpdated: 2026-03-04T11:50:00Z
        status: completed
        turnCount: 1
        tags:
          - feature
          - security
        filesModifiedCount: 2
    totalCount: 1
    offset: 0
    limit: 10
```

## Action Types

Standardized action types (see `action-types.md` for canonical list):

- `edit` — file modification
- `create` — new file creation
- `delete` — file deletion
- `design_decision` — architectural or design choice
- `commit` — git commit (include SHA, branch, message, files)
- `pr_comment` — pull request comment (include PR number, full text)
- `issue_comment` — issue comment (include issue number, full text)
- `web_reference` — internet source consulted (include URL, title, usage)
- `dependency_add` — new dependency added (include name, version, license)
- `license_violation` — banned license detected
- `origin_violation` — banned country of origin detected
- `origin_review` — country of origin could not be determined
- `entity_violation` — banned organization or individual detected
- `copilot_invocation` — server-initiated Copilot call
- `policy_change` — workspace policy configuration change

## Dialog Categories

Valid dialog item categories:

- `reasoning` — agent's internal reasoning and analysis
- `tool_call` — invocation of a tool or function
- `tool_result` — result returned by a tool
- `observation` — observed state or fact
- `decision` — design or implementation decision

## Dialog Roles

Valid dialog item roles:

- `model` — agent/AI model generated content
- `tool` — tool or system generated content
- `system` — system-level messages
- `user` — user-provided content

## Files Created

- `src/McpServer.Repl.Core/ISessionLogWorkflow.cs` — Core workflow interface with state tracking
- `src/McpServer.Repl.Core/SessionLogCommandShapes.cs` — YAML command shapes and parameter/result interfaces
- `src/McpServer.Repl.Core/SessionLogErrorEnvelope.cs` — Structured error envelopes and codes

All interfaces are fully documented with XMLDocs, including canonical identifier rules, turn lifecycle state transitions, error scenarios, and YAML examples.
