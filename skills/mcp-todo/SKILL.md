---
name: mcp-todo
description: Use this skill whenever an agent must list, search, read, create, update, complete, or delete MCP Server TODO items, so it routes every operation through the plugin workflow.todo.* methods, honors the canonical TODO id format, and never edits TODO.yaml or any storage file directly.
license: MIT
---

# MCP Server TODO Management

This skill teaches an agent to query and mutate MCP Server TODO items through the
plugin (`workflow.todo.*`) instead of touching storage files. In this workspace,
"TODO" always means an MCP Server TODO item: not Claude Code's in-session task
list, not a markdown checklist, and not a GitHub issue list.

## When to Use

- The user says: "list todos", "open TODOs", "create a todo", "update todo",
  "mark todo done", "check todo status", "plan implementation", "query tasks".
- You need the current TODO backlog, a single TODO by id, or to record progress
  on an item you are implementing.
- You discover work that must be tracked as a persisted TODO.

## When Not to Use

- Tracking ephemeral, in-session steps for yourself: use the harness task list,
  not MCP TODOs.
- Session logging, requirements (FR/TR), or GraphRAG operations: those are
  separate plugin surfaces (`workflow.sessionlog.*`, requirements docs,
  GraphRAG), not `workflow.todo.*`.
- Any time the only goal is to read `docs/Project/TODO.yaml` "quickly". That is
  still forbidden; use `workflow.todo.query` / `workflow.todo.get`.

## Inputs

- A TODO `id` for get/select/update/delete (canonical format, see Critical Rules).
- For query: optional filters `keyword`, `priority` (`critical|high|medium|low`),
  `section`, and `done` (boolean).
- For create: required `id`, `title`, `section`, `priority`; optional `estimate`,
  `description` (string array), `technicalDetails`, `implementationTasks`
  (`{task, done}`), `note`, `remaining`, `dependsOn`, `functionalRequirements`,
  `technicalRequirements`.
- For update: `id` plus only the fields you want to change (same shape as create,
  plus `done`, `completedDate`, `doneSummary`).

## Critical Rules

1. NEVER read or write `TODO.yaml`, `docs/Project/TODO.yaml`, or any TODO storage
   file directly with Read, Edit, Write, Grep, or shell `cat`. The plugin is the
   only allowed interface. A "quick lookup" is still a violation.
2. Route every operation through the Claude Code plugin
   (`mcpserver-claude-code-plugin`) using `workflow.todo.*` methods. "Not in the
   visible tool list" does not mean unavailable; use the documented wrapper form.
3. Persisted TODO ids must match `^[A-Z][A-Z0-9]*(?:-[A-Z0-9]+)+-\d{3}$`
   (uppercase kebab-case, three-digit suffix) or `^ISSUE-\d+$`. Valid:
   `PLAN-NAMINGCONVENTIONS-001`, `MCP-TODO-CREATE-001`, `ISSUE-17`. Invalid:
   `plan-api-001`, `MCP-API-42`, `MCP-001`, `ISSUE-ABC`.
4. To create a GitHub-backed TODO, submit the special id `ISSUE-NEW`. The server
   creates the issue, rewrites the saved id to the canonical `ISSUE-{number}`,
   and returns that id. After first sync, `ISSUE-{number}` descriptions are
   immutable; updates surface to GitHub as a comment, not a body rewrite.
5. Each `dependsOn` entry must itself be a valid persisted TODO id.
6. Do not invent methods or fields. The only TODO methods are listed in Workflow.
7. Every request needs a unique `requestId`, format
   `req-<yyyyMMddTHHmmssZ>-<seq-or-slug>`, unique within the session.

## Workflow

1. Ensure the session is bootstrapped and the plugin path is verified (see
   `CLAUDE.md` "MCP Session Logging"). If signature/health verification fails,
   log `MCP_UNTRUSTED` and stop touching MCP TODO endpoints.
2. List or search: call `workflow.todo.query` with the desired filters. Omit
   `params` (or send `{}`) to list everything; pass `done: false` for open items.
   The result is `{ items: [...], totalCount }`.
3. Read one item: call `workflow.todo.get` with `params.id`. The result is
   `{ item: {...} }` (full `TodoFlatItem`).
4. Optionally set the active item with `workflow.todo.select` (`params.id`) so
   `workflow.todo.updateSelected` can patch it without repeating the id.
5. Create: call `workflow.todo.create` with the required fields. Choose a
   canonical id up front, or use `ISSUE-NEW` for a GitHub-backed item and read
   back the rewritten `ISSUE-{number}` from the response.
6. Update progress: call `workflow.todo.update` with `params.id` plus only the
   changed fields (for example `implementationTasks`, `remaining`, `note`). To
   mark complete, set `done: true` and provide `doneSummary` (and optionally
   `completedDate`). `workflow.todo.updateSelected` applies the same patch to the
   selected item.
7. Delete only when truly removing the item: `workflow.todo.delete` with
   `params.id`.
8. Generate guidance when planning or implementing: `workflow.todo.streamPlan`,
   `workflow.todo.streamImplement`, or `workflow.todo.streamStatus` (each takes
   `params.id` and emits `workflow.todo.streamStatus` events).
9. Attach requirements you discover via the update path
   (`functionalRequirements`, `technicalRequirements`) and also record FR/TR ids
   in your session log turn tags.
10. After a state-changing call, update the session log turn with the action and
    the affected TODO id.

### Reference request shape

```yaml
type: request
payload:
  requestId: req-20260618T101500Z-001-list-open-todos
  method: workflow.todo.query
  params:
    done: false
    priority: high
```

The equivalent REST surface (for read-only diagnosis only, never to bypass the
plugin) is `GET/POST/PUT/DELETE /mcpserver/todo[/{id}]` with the workspace
`X-Api-Key` and `X-Workspace-Path` headers from `AGENTS-README-FIRST.yaml`.

## Validation Checklist

- Did you use a `workflow.todo.*` method and not open TODO.yaml directly?
- Is the `id` valid against the canonical regex (or `ISSUE-NEW` for a new issue)?
- For create, are `id`, `title`, `section`, and `priority` all present and is
  `priority` one of `critical|high|medium|low`?
- For update, did you include `id` and only the fields that actually changed?
- For completion, did you set `done: true` with a `doneSummary`?
- Are all `dependsOn` ids valid persisted TODO ids?
- Is the `requestId` unique and correctly formatted?
- Did you log the action and TODO id to the session log afterward?

## Common Pitfalls

- Reading or editing `docs/Project/TODO.yaml` "just to check": forbidden; query
  instead.
- Lowercase or short ids like `mcp-001` or `MCP-API-42`: they fail the regex and
  are rejected.
- Re-creating an item that already exists instead of using
  `workflow.todo.update`.
- Trying to rewrite an `ISSUE-{number}` description after first sync: the body is
  immutable; the server records changes as a GitHub comment.
- Sending an entire object on update when only one field changed, which can
  clobber fields you did not mean to touch.
- Confusing MCP TODOs with the harness task list, GitHub issues, or a markdown
  checklist.
