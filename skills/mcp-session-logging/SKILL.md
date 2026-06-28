---
name: mcp-session-logging
description: Use this skill whenever an agent does meaningful work in an McpServer workspace and must record a session-log turn (begin a turn before work, append dialog, actions, and design decisions, then complete or fail the turn) through the mcpserver plugin workflow.sessionlog.* methods, never by editing session-log files directly.
license: MIT
---

# MCP Session Logging

Record every meaningful unit of work as an McpServer session-log turn using the
`mcpserver` plugin `workflow.sessionlog.*` methods. A turn is begun before work
starts, enriched with dialog, actions, and design decisions while work proceeds,
and then completed (success) or failed (error). All session state is owned by the
McpServer; you interact with it only through the plugin API.

## When to Use

- You are about to start work on a user message or task inside an McpServer workspace.
- You finished a meaningful change (edit, commit, decision, requirement, blocker) and need to persist it.
- You need to record reasoning, tool output, or a design decision for audit.
- You are wrapping up or aborting work and must close the active turn.
- You need to review recent session history before acting (`queryHistory`).

## When Not to Use

- Trivial read-only lookups that the parent turn already covers (do not open a new turn per file read).
- TODO CRUD (use the TODO skill / `workflow.todo.*`), requirements (`workflow.requirements.*`), or GraphRAG work; those have their own skills.
- Any situation where signature verification, `/health`, or nonce verification has failed: log `MCP_UNTRUSTED` and continue without the MCP server.

## Inputs

- The active marker file `AGENTS-README-FIRST.yaml` in the workspace root (API key, endpoints, base URL). Re-read it on any 401.
- Your real agent identity in Pascal-Case (for example `ClaudeCode`, `Copilot`), used as the `agent` / `sourceType` prefix.
- The current model name (for example `claude-opus-4-8`).
- The user's prompt or task text (for `queryTitle` / `queryText` and for the request-id slug).

## Critical Rules

- NEVER read, edit, or write session-log files (or `todo.yaml`) directly with Read, Edit, Write, Grep, or shell. The plugin API is the only allowed interface.
- Begin the turn BEFORE doing the work, not after.
- Persist immediately after each meaningful change. Do not batch saves to the end.
- Use your real Pascal-Case agent identity. Never use placeholder or misleading `sourceType` values.
- Session id format: `<Agent>-<yyyyMMddTHHmmssZ>-<suffix>`; must start with the exact `agent` prefix (case-sensitive). Regex: `^[A-Z][A-Za-z0-9]*-\d{8}T\d{6}Z-[a-z0-9]+(?:-[a-z0-9]+)*$`.
- Request id format: `req-<yyyyMMddTHHmmssZ>-<seq>-<slug-from-prompt>` where `yyyyMMddTHHmmssZ` is the UTC time the turn begins, `<seq>` is a zero-padded 3-digit per-session counter (`001`, `002`, ...), and `<slug>` is a lowercase kebab-case truncation (~40 chars) of the prompt with articles and filler dropped. Regex: `^req-\d{8}T\d{6}Z-[a-z0-9]+(?:-[a-z0-9]+)*$`.
- The `requestId` passed to `beginTurn` is the turn's identity; reuse that SAME `requestId` for `appendDialog`, `appendActions`, `updateTurn`, `completeTurn`, and `failTurn` so they bind to the turn.
- Log design decisions twice: as an `appendDialog` item with `category: decision`, and as an `appendActions` action with `type: design_decision`.
- A completed or failed turn is immutable (`turn_immutable`); make all updates before closing it.
- No em-dashes in any logged text.

## Workflow

1. Session start (once per session): read `AGENTS-README-FIRST.yaml`, then call
   `workflow.sessionlog.bootstrap` (params `{}`) to initialize the subsystem.
2. Open or resume the session. Call `workflow.sessionlog.openSession` with
   `agent`, a canonical `sessionId`, `title`, and `model`. Use
   `workflow.sessionlog.currentSession` to check for an already-open session, and
   `workflow.sessionlog.queryHistory` (`agent`, `limit`, `offset`) to review
   recent work before starting.
3. Begin a turn BEFORE working. Call `workflow.sessionlog.beginTurn` with a
   canonical `requestId`, plus `queryTitle` and `queryText` describing the user's
   request. Record this `requestId`; it identifies the turn for the rest of step 4-6.
4. While working, persist progress as it happens (reuse the turn `requestId`):
   - `workflow.sessionlog.appendDialog` with `dialogItems`, each item having
     `role` (`model` | `tool` | `system` | `user`), `content`, and `category`
     (`reasoning` | `tool_call` | `tool_result` | `observation` | `decision`).
   - `workflow.sessionlog.appendActions` with `actions`, each having `order`,
     `description`, `type` (see `docs/context/action-types.md`, for example
     `create`, `edit`, `commit`, `design_decision`, `web_reference`), `status`
     (`completed` | `in_progress` | `failed`), and `filePath` (or empty string).
   - `workflow.sessionlog.updateTurn` to set/refresh `interpretation`, `response`,
     `tags`, `contextList`, `filesModified`, `designDecisions`,
     `requirementsDiscovered`, `blockers`, and `tokenCount`.
5. For each design decision: append a dialog item with `category: decision`
   (what was decided, alternatives, rationale, affected requirements) AND an
   action with `type: design_decision`.
6. Close the turn exactly once:
   - Success: `workflow.sessionlog.completeTurn` with a final `response` summary.
   - Failure/abort: `workflow.sessionlog.failTurn` with `errorMessage` (required,
     non-empty) and optional `errorCode`.
7. Compaction safety: before any compaction step, call `updateTurn` to persist
   current state; after compaction, append an `observation` dialog item recording
   the outcome and persist again.

## Validation Checklist

- [ ] `bootstrap` ran this session before any state-changing call.
- [ ] A session is open with a canonical `sessionId` that starts with your real Pascal-Case agent prefix.
- [ ] `beginTurn` ran BEFORE the work, with a canonical `requestId`.
- [ ] The same turn `requestId` was reused for every dialog/action/update/close call.
- [ ] Each meaningful change was persisted immediately (no deferred saves).
- [ ] Every design decision appears both as a `category: decision` dialog item and a `type: design_decision` action.
- [ ] `filesModified`, `contextList`, and `requirementsDiscovered` reflect actual work.
- [ ] The turn was closed exactly once via `completeTurn` (success) or `failTurn` with a non-empty `errorMessage` (failure).
- [ ] No session-log or `todo.yaml` file was read or written directly.
- [ ] No em-dashes in any logged text.

## Common Pitfalls

- `invalid_session_id` / `invalid_request_id`: the id violated the canonical regex (wrong case prefix, hyphenated date like `2026-03-04`, missing `req-` prefix, or uppercase/illegal chars in the slug).
- `turn_immutable`: you tried to update or append after `completeTurn`/`failTurn`. Make all edits before closing.
- `turn_not_found` / `invalid_turn_state`: you appended or completed without an active turn; call `beginTurn` first.
- `turn_already_exists`: a turn with that `requestId` already exists; bump the `<seq>` segment.
- `session_not_found`: no open session; run `openSession` (or `currentSession`) first.
- 401 from the server: the API key rotated on restart; re-read `AGENTS-README-FIRST.yaml` and retry.
- Closing the turn with no `response` summary, or failing without an `errorMessage`, loses the audit trail and (for `failTurn`) is rejected by validation.
- Editing the session-log YAML directly to "fix" something: forbidden; use `updateTurn` or the documented section edit endpoints through the plugin instead.

## YAML Mutation Rule

When YAML must be changed, deserialize the complete document into an object, mutate the object, serialize the object, and save the result. Do not append YAML snippets, replace YAML lines, remove YAML lines, or build YAML payloads as strings. For PowerShell work, use `plugins/core/lib-ps/yaml-object-mutation.ps1` and call `Set-McpYamlObjectValue` or `Update-McpYamlObject`.
