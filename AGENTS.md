# Agent Instructions

## Session Start

1. Read `AGENTS-README-FIRST.yaml` in the repo root for the current API key and endpoints.
2. For specific operational steps (session bootstrap, session log turn workflow, and helper command sequence), follow `AGENTS-README-FIRST.yaml`.

On every subsequent user message:

1. Follow `AGENTS-README-FIRST.yaml` for specific operational instructions.
2. Complete the user's request.

## Rules

1. `templates/prompt-templates.yaml` (`default-marker-prompt`) is the source of truth for specific agent instructions. `AGENTS-README-FIRST.yaml` is the rendered runtime instruction set.
2. Keep this file focused on durable workspace policy and conventions; avoid duplicating marker-file operational procedures.
3. Use helper modules for session log and TODO operations. Do not make raw API calls.
4. Persist session log updates immediately after each meaningful change (turn creation, action append, decision, requirement, blocker, file/context update). Do not defer saves.
5. Capture rich turn detail: interpretation, response, status, actions (type/status/filePath), contextList, filesModified, designDecisions, requirementsDiscovered, blockers, and relevant processing dialog.
6. Follow workspace conventions in `.github/copilot-instructions.md` for build, test, and architecture guidance.
7. Use **only `pwsh.exe`** for shell commands and script execution; do not use `powershell.exe`.
8. When you need API schemas, module examples, or compliance rules, load them from `docs/context/` or use `context_search`.
9. Do not fabricate information. If you made a mistake, acknowledge it. Distinguish facts from speculation.
10. Prioritize correctness over speed. Do not ship code you have not verified compiles and is logically sound.
11. When writing session logs or other audit records, agents must identify themselves accurately using their real agent identity in Pascal-Case. Do not use placeholder, legacy, or misleading sourceType values.
12. Never edit YAML by appending, replacing, or removing text lines. Deserialize the complete document into an object, mutate the object, serialize it, and save it. Use `plugins/core/lib-ps/yaml-object-mutation.ps1` for PowerShell work.

## Byrd Test Gate

The canonical process is `docs/Development-Process-draft-v4.md`. To leave a Byrd implementation slice, the entire unit test suite for the current iteration and previous iterations must be completely passing. In this workspace, skipped tests are not passing tests: a validation gate requires zero failures and zero skips in the executed scope. Tests should directly track progress; deferred work belongs in MCP TODO/requirements state until that slice starts, not in skipped test placeholders.

## Where Things Live

- `AGENTS-README-FIRST.yaml` — connection details, API key, workspace config (regenerated on server start)
- `.github/copilot-instructions.md` — build/test commands, architecture overview, coding conventions
- `docs/context/` — on-demand reference docs (schemas, module docs, compliance rules, action types)
- `docs/Project/` — requirements docs, TODO.yaml, mapping matrices
- `templates/` — prompt templates (loaded on demand)
- `tools/powershell/McpContext.psm1` — PowerShell module for context ingestion/query workflows
- `tools/powershell/McpContext.USER.md` — user-level guide for the McpContext module
- `tools/powershell/McpContext.AGENT.md` — agent workflow instructions for the McpContext module
- `plugins/core/lib-ps/yaml-object-mutation.ps1` — object-first YAML mutation helper for agents and plugin maintenance

## MCP Interaction via REPL Tools

Agents running inside `McpAgent` must use the 27 built-in tools instead of raw HTTP calls. See `docs/REPL-MIGRATION-GUIDE.md` for the full tool inventory and migration patterns.

Key rules:
- Use `mcp_session_*` tools for session log lifecycle (bootstrap, turns, history).
- Use `mcp_todo_*` tools for TODO CRUD (query, get, create, update, delete, plan, status, implementation).
- Use `mcp_requirements_*` tools for FR/TR/TEST queries.
- Use `mcp_client_invoke` for any sub-client method not covered by a dedicated tool (context search, GitHub, workspace, etc.).
- Do not make raw HTTP calls to `/mcpserver/*` endpoints when a tool is available.

## Context Loading by Task Type

- Session logging → `docs/context/session-log-schema.md` + `docs/context/module-bootstrap.md`
- TODO management → `docs/context/todo-schema.md` + `docs/context/module-bootstrap.md`
- API integration → `docs/context/api-capabilities.md` (or `GET /swagger/v1/swagger.json`)
- Adding dependencies → `docs/context/compliance-rules.md`
- Logging actions → `docs/context/action-types.md`
- New to workspace → this file + `docs/context/api-capabilities.md`
- Migrating from raw API → `docs/REPL-MIGRATION-GUIDE.md`

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
- Add source URLs to the turn's contextList array.
- Attribute external code in both the session log and code comments.

## Requirements Tracking

When you discover or agree on new requirements during a session:

1. Update the files in `docs/Project/`:
   - `Functional-Requirements.md` — append FR-MCP-* entries
   - `Technical-Requirements.md` — append TR-MCP-* entries
   - `TR-per-FR-Mapping.md` — append mapping rows
   - `Requirements-Matrix.md` — append status rows
   - `Testing-Requirements.md` — append TEST-MCP-* entries
2. Include the requirement ID in your session log turn's tags.
3. Capture requirements as they emerge. Do not defer to later.

## Design Decision Logging

When a design decision is made:

1. Log it as a session log dialog item with category "decision".
2. Include: the decision, alternatives considered, rationale, and affected requirements.
3. Add a session log action with type "design_decision".
4. If the decision affects existing code or requirements, note what needs updating.

## Session Continuity

At the start of every session:

1. Follow the session-start checklist in `AGENTS-README-FIRST.yaml`.
2. Read `docs/Project/Requirements-Matrix.md` to understand project state.
3. If resuming interrupted work, review the last session's pending decisions.

At regular intervals during long sessions (~10 interactions):

1. Follow marker-file update cadence and session logging requirements from `AGENTS-README-FIRST.yaml`.
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

