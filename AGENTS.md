# Agent Instructions

## Session Start

1. Read `AGENTS-README-FIRST.yaml` in the repo root for the current API key and endpoints.
2. Verify the MCP server is running: `GET /health`.
3. Bootstrap helper modules from the Tool Registry (see `docs/context/module-bootstrap.md`).
4. Review recent session history: `Get-McpSessionLog -Limit 5` or `mcp_session_query 5`.
5. Review current tasks: `Get-McpTodo` or `mcp_todo_list`.
6. Post a session log entry before starting work on the user's request.

On every subsequent user message:

1. Post a session log entry before starting work.
2. Complete the user's request.
3. Update the entry with results, actions taken, and files modified.

## Rules

1. Post a session log entry before any work on a user request. Update it with results when done.
2. Use helper modules for session log and TODO operations. Do not make raw API calls — the modules handle workspace routing automatically.
3. Write decisions, requirements, and state to the session log, not just conversation.
4. Follow workspace conventions in `.github/copilot-instructions.md` for build, test, and architecture guidance.
5. When you need API schemas, module examples, or compliance rules, load them from `docs/context/` or use `context_search`.
6. Do not fabricate information. If you made a mistake, acknowledge it. Distinguish facts from speculation.
7. Prioritize correctness over speed. Do not ship code you have not verified compiles and is logically sound.

## Where Things Live

- `AGENTS-README-FIRST.yaml` — connection details, API key, workspace config (regenerated on server start)
- `.github/copilot-instructions.md` — build/test commands, architecture overview, coding conventions
- `docs/context/` — on-demand reference docs (schemas, module docs, compliance rules, action types)
- `docs/Project/` — requirements docs, TODO.yaml, mapping matrices
- `templates/` — prompt templates (loaded on demand)

## Context Loading by Task Type

- Session logging → `docs/context/session-log-schema.md` + `docs/context/module-bootstrap.md`
- TODO management → `docs/context/todo-schema.md` + `docs/context/module-bootstrap.md`
- API integration → `docs/context/api-capabilities.md` (or `GET /swagger/v1/swagger.json`)
- Adding dependencies → `docs/context/compliance-rules.md`
- Logging actions → `docs/context/action-types.md`
- New to workspace → this file + `docs/context/api-capabilities.md`

## Agent Conduct

You represent the workspace owner. Your work directly reflects the owner's professional reputation.

### Honesty

- Do not fabricate information, capabilities, or results.
- Distinguish between facts, informed opinions, and speculation.
- Acknowledge mistakes immediately and correct them.

### Correctness

- Prioritize correctness over speed.
- When uncertain, state your uncertainty and suggest verification steps.
- Prefer proven patterns over clever approaches unless directed otherwise.
- All code must have XMLDocs. All public APIs must be documented.
- Follow DRY, SOLID, and existing project conventions.

### Decision Documentation

- Log every decision to the session log, including trivial ones.
- For each decision, document: what was decided, why, what alternatives were considered, what was rejected.
- Log design decisions as dialog entries with category "decision" and as session log actions with type "design_decision".

### Professional Representation

- Every interaction is audited via the session log.
- Every commit must be correct, clean, well-described, and complete.
- Log all commits as actions with type "commit" (SHA, branch, message, files).
- Log all PR/issue comments as actions with type "pr_comment" or "issue_comment".

### Source Attribution

- Document all web sources in the session log as actions with type "web_reference" (URL, title, usage).
- Add source URLs to the entry's contextList array.
- Attribute external code in both the session log and code comments.

## Requirements Tracking

When you discover or agree on new requirements during a session:

1. Update the files in `docs/Project/`:
   - `Functional-Requirements.md` — append FR-MCP-* entries
   - `Technical-Requirements.md` — append TR-MCP-* entries
   - `TR-per-FR-Mapping.md` — append mapping rows
   - `Requirements-Matrix.md` — append status rows
   - `Testing-Requirements.md` — append TEST-MCP-* entries
2. Include the requirement ID in your session log entry's tags.
3. Capture requirements as they emerge. Do not defer to later.

## Design Decision Logging

When a design decision is made:

1. Log it as a session log dialog entry with category "decision".
2. Include: the decision, alternatives considered, rationale, and affected requirements.
3. Add a session log action with type "design_decision".
4. If the decision affects existing code or requirements, note what needs updating.

## Session Continuity

At the start of every session:

1. Read `AGENTS-README-FIRST.yaml` for connection details.
2. Query recent session logs (limit 5) for context.
3. Query current TODOs.
4. Read `docs/Project/Requirements-Matrix.md` to understand project state.
5. If resuming interrupted work, review the last session's pending decisions.

At regular intervals during long sessions (~10 interactions):

1. Push an updated session log with all entries so far.
2. Ensure all design decisions are captured.
3. Verify requirements docs are up to date.

## Glossary

- **MCP** — Model Context Protocol, an open standard for tool-calling between AI agents and context servers.
- **Workspace** — a project directory registered with the MCP server. All workspaces share a single port; use the `X-Workspace-Path` header to target a specific one.
- **Marker File** — the `AGENTS-README-FIRST.yaml` file at each workspace root. Contains connection details, auth token, and agent prompt.
- **API Key** — a per-workspace cryptographic token that rotates on each server restart. Required for all `/mcpserver/*` REST endpoints.
- **Streamable HTTP** — the MCP wire protocol transport at `/mcp-transport`. Carries JSON-RPC tool calls over HTTP POST with streaming responses.
- **Session Log** — an audit record of every agent interaction, stored per-session with full request/response history.
- **Context Pack** — an ordered set of document chunks retrieved by semantic + full-text hybrid search, scoped to the workspace.
- **Tool Bucket** — a GitHub repository containing tool manifest files, similar to a Scoop package bucket.

## Response Formatting

- Do not use table-style output in responses.
- Use concise bullets or short paragraphs instead.
