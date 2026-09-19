# Project Management with McpServer

**Audience:** operators and coding agents running project work through McpServer  
**Sources:** McpServer `main` (`docs/USER-GUIDE.md`, `docs/MCP-SERVER.md`, `docs/Development-Process-draft-v4.md`, `docs/byrd-todo-execution-spec.md`, `AGENTS.md`) plus `mcpserver-codex-plugin` and `mcpserver-grok-plugin`  
**CLI coverage:** OpenAI **Codex CLI** (`codex`) and xAI **Grok Build CLI** (`grok` / `agent`)  
**Generated:** 2026-09-19T02:15Z

---

## 1. What McpServer gives you for project management

McpServer is the **authoritative store** for:

| Concern | Surface |
|---|---|
| Work items | `todo_*` / `/mcpserver/todo` |
| Requirements | FR / TR / TEST + mappings + acceptance criteria |
| Use cases | Use Case Manager (`usecase_*`, `/usecase/`) |
| Continuity | Session log turns (`sessionlog_*`) |
| Process | Byrd Development Process v4 + hostile validation |
| Optional sharing | Products (`PROD-*`) for cross-workspace effective requirements |

**Do not** hand-edit `docs/Project/TODO.yaml` or session-log YAML as the write path. Mutate through MCP tools / plugin helpers / PowerShell modules; YAML mutation must be deserialize \u2192 mutate object \u2192 serialize.

Projections under `docs/Project/` are **exports**, not the source of truth.

---

## 2. Trust bootstrap (every agent, every machine)

Before any TODO / session / requirements write:

1. Read workspace root `AGENTS-README-FIRST.yaml` (regenerated on server start).
2. Verify marker HMAC with the **full** workspace API key (default `/api-key` may be read-only).
3. Call `GET /health?nonce=...` and confirm the nonce echoes.
4. On failure: treat as `MCP_UNTRUSTED`, stop MCP writes, continue local-only with an explicit note.

Auth on `/mcpserver/*`: header `X-Api-Key`. Optional `X-Workspace-Path` for explicit workspace targeting.

Default local base URL: `http://localhost:7147`  
MCP transport: `http://localhost:7147/mcp-transport` (Streamable HTTP).

---

## 3. Byrd Development Process v4 (how work moves)

Canonical: `docs/Development-Process-draft-v4.md`  
Checklist skill: `skills/byrd-tdd-process`  
TODO-centered execution: `docs/byrd-todo-execution-spec.md`

### Non-negotiable loop

1. **Planning** \u2014 FR / TR / TEST + acceptance criteria + use cases **before** schema/product code.
2. **Hostile H0** \u2014 AGREE before implementation slices.
3. Per slice: **Red** (AC unit tests) \u2192 **mocks-first** \u2192 **Green** \u2192 **Refactor** \u2192 full unit suite **Failed 0 / Skipped 0**.
4. **Validation** (integration) only after units green.
5. Per-slice **H-red / H-green** hostile AGREE; **H-done** before `Done: true`.
6. Deploy via Nuke `UpdateService` **only when an operator asks**.

Skipped tests are **not** passes. Deferred work belongs in MCP TODO/requirements, not skipped tests.

### Hostile validation

- Adversarial Grok (or designated) sub-agent skill `hostile-validator` when installed.
- Receipts: `docs/receipts/hostile-validator-\u003cutc\u003e.md` + `.json`.
- Default posture: FAIL until independently re-verified.
- Operator may raise the AGREE bar (e.g. Accuracy **and** Completeness \u2265 98).

PLACEHOLDER_CONTINUE