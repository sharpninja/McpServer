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

**Do not** hand-edit `docs/Project/TODO.yaml` or session-log YAML as the write path. Mutate through MCP tools / plugin helpers / PowerShell modules; YAML mutation must be deserialize → mutate object → serialize.

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

1. **Planning** — FR / TR / TEST + acceptance criteria + use cases **before** schema/product code.
2. **Hostile H0** — AGREE before implementation slices.
3. Per slice: **Red** (AC unit tests) → **mocks-first** → **Green** → **Refactor** → full unit suite **Failed 0 / Skipped 0**.
4. **Validation** (integration) only after units green.
5. Per-slice **H-red / H-green** hostile AGREE; **H-done** before `Done: true`.
6. Deploy via Nuke `UpdateService` **only when an operator asks**.

Skipped tests are **not** passes. Deferred work belongs in MCP TODO/requirements, not skipped tests.

### Hostile validation

- Adversarial Grok (or designated) sub-agent skill `hostile-validator` when installed.
- Receipts: `docs/receipts/hostile-validator-<utc>.md` + `.json`.
- Default posture: FAIL until independently re-verified.
- Operator may raise the AGREE bar (e.g. Accuracy **and** Completeness ≥ 98).

---

## 4. Core project objects

### 4.1 TODOs

- Create / list / get / update via MCP `todo_*` or REST `/mcpserver/todo`.
- Attach FR / TR ids on the TODO; keep `implementationTasks` as structured tasks with `done` flags.
- Prefer streaming plan/implement/status tools when the plugin exposes them (`todo_plan`, `todo_implement`, `todo_status`, checkpoints).
- `Done: true` only after hostile **H-done** AGREE and a receipt path in `doneSummary`.

PowerShell (from `main` USER-GUIDE pattern):

```powershell
$todo = Get-McpTodo -Id "MCP-USERDOCS-001"
$tasks = @(
  @{ task = "Write Installation & Prerequisites guide"; done = $true },
  @{ task = "Write Configuration reference"; done = $true }
)
Update-McpTodo -Id $todo.id -ImplementationTasks $tasks
```

### 4.2 Requirements

- Kinds: `fr`, `tr`, `test`, `mapping`.
- Structured `acceptanceCriteria`: `{ id, text, isSatisfied, evidence? }`.
- Map every FR → TR → TEST; run `ValidateTraceability` before leaving Planning.
- Export wiki / `docs/Project` via `requirements_generate` when needed for humans; store remains authoritative.

### 4.3 Use cases

- Create with `usecase_create`; link FR via `frId` + `Realizes`.
- Cover **100%** of in-scope FRs before H0 when the plan requires it.
- UI: Use Case Manager at `/usecase/` when packaged.

### 4.4 Session log

- Open a session and begin a turn with `planFile` + `todoId` (`None` when none).
- Append dialog and actions for durable decisions.
- Identity: Pascal-Case agent sourceType (e.g. `Codex`, `GrokCode`).
- Persist after meaningful changes; do not defer saves.

Session id pattern examples:

- Codex: `Codex-YYYYMMDDTHHMMSSZ-slug`
- Grok: `GrokCode-YYYYMMDDTHHMMSSZ-slug`

---

## 5. Shared CLI setup (Codex + Grok)

### 5.1 Run McpServer

```bash
# Health
curl -sS http://127.0.0.1:7147/health

# MCP transport (Streamable HTTP)
# http://127.0.0.1:7147/mcp-transport
```

Both CLIs should point an MCP server entry at that URL (see plugin `.mcp.json`).

### 5.2 Install CLIs

```bash
# Codex
npm install -g @openai/codex
# or: npm config set prefix ~/.npm-global && npm install -g @openai/codex

# Grok Build
curl -fsSL https://x.ai/cli/install.sh | bash
export PATH="$HOME/.grok/bin:$PATH"
grok --version
codex --version
```

### 5.3 Authenticate

```bash
# Codex (browser or API key per Codex docs)
codex login

# Grok (browser, device code, or API key)
grok login
# headless:
grok login --device-code
# or:
export XAI_API_KEY="xai-..."
```

### 5.4 Register McpServer MCP

**Codex**

```bash
codex mcp  # list / manage external MCP servers
# Ensure an entry equivalent to:
# type: http, url: http://localhost:7147/mcp-transport
```

Plugin path: clone/symlink `mcpserver-codex-plugin`; follow its `setup.sh` / `AGENTS.md`.

**Grok**

```bash
# Symlink plugin skills
ln -s /path/to/mcpserver-grok-plugin/skills/* ~/.grok/skills/

# Confirm MCP
grok mcp doctor mcpserver   # when available in your build
# .mcp.json should expose http://localhost:7147/mcp-transport
```

Keep `mcpserver-repl` (dotnet global tool) on PATH for workflow shim parity when skills call REPL helpers.

---

## 6. Day-to-day PM workflow (same on both CLIs)

Use this sequence whether the human is driving **Codex** or **Grok**:

1. **Bootstrap** marker + health nonce.
2. **Open session** for the workspace; begin turn tied to plan + TODO.
3. **Plan in the store** — author/update FR/TR/TEST/AC/use cases; freeze matrix; ValidateTraceability.
4. **Hostile H-plan / H0** — AGREE receipts under `docs/receipts/`.
5. **Execute slices** — Red → mocks-first → Green → Refactor → H-red/H-green.
6. **Update TODO** via tools after each slice (tasks, remaining, notes). Never claim done without H-done.
7. **Export** requirements/wiki when stakeholders need markdown; do not treat export as editable truth.
8. **Close** only with H-done AGREE + `Done: true` + receipt citation.

### Prompt starters (paste into either CLI)

**Start of day**

> Read AGENTS-README-FIRST.yaml, verify health nonce, open a session, `todo_list` open high items, summarize blockers.

**Planning a feature**

> For TODO &lt;ID&gt;, draft FR/TR/TEST with dense acceptance criteria and use cases covering every FR. Write them with MCP requirements/usecase tools. Do not write product schema until H0 AGREE.

**Slice execution**

> Execute Byrd slice S&lt;n&gt; for TODO &lt;ID&gt;: Red AC tests first (Failed≠0 on new tests, Skipped=0), then mocks-first, then Green. Cite FR/TR/TEST in XML docs. Stop for H&lt;n&gt;-red before Green if the plan requires a separate hostile gate.

**Status report**

> Report TODO state, open hostile gates, latest receipt paths, and whether ValidateTraceability / unit gates are green. Do not mark Done.

---

## 7. Codex-specific notes

- Prefer Codex MCP management (`codex mcp`) so `todo_*`, `requirements_*`, `sessionlog_*`, `usecase_*` appear as tools.
- Non-interactive runs: `codex exec "..."`.
- Session continuity: follow `mcpserver-codex-plugin` marker + helper rules; use Pascal-Case `Codex` as sourceType.
- Sandbox: use Codex sandbox flags when running builds/tests; still require Failed 0 Skipped 0 for Byrd gates.
- Do not bypass MCP by editing `TODO.yaml` when tools work.

---

## 8. Grok-specific notes

- Load `mcpserver-grok-plugin` skills into `~/.grok/skills`.
- Single-turn checks: `grok -p "..."` (headless).
- Interactive TUI: `grok` or `agent`.
- Session naming: `GrokCode-...` / `req-...` per GROK-USAGE.md.
- If TUI lacks a workflow tool but has shell, call plugin `lib/repl-invoke.ps1` / `mcpserver-repl` with workflow methods.
- Hostile validation briefs should spawn a separate adversarial agent identity (`GrokSubagentHostile`), not self-certify.

---

## 9. Dual-CLI collaboration pattern

Use **both** CLIs on the same McpServer workspace:

| Role | CLI | Typical work |
|---|---|---|
| Implementer | Codex **or** Grok | Red/Green code, TODO updates, session turns |
| Reviewer / hostile | The **other** CLI (or Grok hostile sub-agent) | H-plan/H0/H-red/H-green receipts |
| Docs / export | Either | `requirements_generate`, USER-GUIDE updates |

Rules:

1. One writer to a TODO mutation at a time; refresh `todo_get` before update.
2. Hostile AGREE must come from an adversarial pass, not the implementer alone.
3. Share receipt paths in TODO `note` / `doneSummary`.
4. Same MCP endpoint and API key for both; do not fork requirements into chat-only notes.

Example split for a feature TODO:

1. Grok authors Planning (FR/TR/TEST/UC) into the store.
2. Codex runs H-plan / H0 hostile (or vice versa).
3. Codex implements S1 Red/Green.
4. Grok runs H1-red then H1-green.
5. Continue until H-done; either CLI may flip `Done: true` **only** after AGREE.

---

## 10. Operator checklist

- [ ] McpServer healthy on :7147; Postgres (or configured provider) reachable  
- [ ] Full API key from marker (not read-only)  
- [ ] Codex CLI installed + logged in + MCP → `/mcp-transport`  
- [ ] Grok CLI installed + logged in + skills + MCP → `/mcp-transport`  
- [ ] `mcpserver-repl` available when plugins need workflow shim  
- [ ] Byrd v4 + hostile receipt folder writable (`docs/receipts/`)  
- [ ] Agents never hand-edit TODO.yaml / session YAML  

---

## 11. References (main branch)

- `docs/USER-GUIDE.md` — install, config, PowerShell modules, REST map  
- `docs/MCP-SERVER.md` — server overview, products, build  
- `docs/Development-Process-draft-v4.md` — Byrd v4  
- `docs/byrd-todo-execution-spec.md` — TODO tool contracts  
- `AGENTS.md` — durable agent policy  
- `mcpserver-codex-plugin/` — Codex integration  
- `mcpserver-grok-plugin/GROK-USAGE.md` — Grok CLI/TUI usage  

---

## 12. Revision notes

- Guide authored against McpServer **`main`** documentation snapshots plus current Codex/Grok plugin contracts.
- Live dual-CLI authorship of this file requires authenticated `codex` and `grok` sessions; install paths verified: Codex `0.155.x`, Grok Build `1.0.x`.
