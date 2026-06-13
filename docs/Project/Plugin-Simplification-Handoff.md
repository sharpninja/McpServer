# McpServer Ecosystem Simplification - Handoff

Branch: `feature/sessionlog-lifecycle` (worktree `F:\GitHub\McpServer-lifecycle`, based on `main@95ba268`).
Plan of record: `C:\Users\kingd\.claude\plans\breezy-prancing-feigenbaum.md`.

## What is DONE and validated on this branch

| Phase | Scope | Commits | Validation |
|---|---|---|---|
| 0 | SessionLog workspace-stamping bugs (A dup-key 500, B severed commit assoc, C turn count); drop child query filters; parent-inheritance stamping invariant; `RepairWorkspaceStampsAsync` + `POST /mcpserver/sessionlog/repair-workspace-stamps` (dryRun). | `37a3c78` (on `main`, deployed live) | SQLite relational tests; full solution suite; live repro of all three bugs passed; fleet drift count 0. |
| 1a | Stateless session lifecycle: `POST {agent}/{sessionId}/open\|{requestId}/begin\|complete\|fail`; `SessionLogClient` methods; MCP tool adapters. Additive partial submits (`MapDtoToEntity` merge-on-null; `UpsertTurns` mergeOmittedFields). | `ea6b23a` | 8 new tests; unit 988, integration 201, client 197. |
| 1b | REPL framing: NDJSON single-line fast path + `---` response terminator. Node ReplBridge becomes correct with zero plugin changes. | `208be04` | 4 framing tests; Repl.Core 637; Repl.Integration 162; live replay against branch-built Repl.Host. |
| 1c | `workflow.*` namespaces marked `deprecated:true`; `workflow.sessionlog` lifecycle verbs with explicit ids route statelessly through the client, bypassing `SessionLogState`. | `a798ddf` | 7 dispatcher/serializer tests; Repl.Core 637; Repl.Integration 162. |
| 1d | Split 2501-line `McpServerMcpTools.cs` into 6 verbatim domain partials (base + Context/Todo/Requirements/SessionLog/GitHub). | `5b20769` | byte-identical regions; 80 `[McpServerTool]` before/after; build 0 warn; unit 988, integration 201. |
| 2 (core authored) | `plugins/core/`: canonical `lib-sh` (18), `lib-ps` (7), `lib-node` TS package (15 src), `hooks-templates`, `sync/` (sync + sha256 checksum guard, sh+ps1), `ci-templates/core-guard.yml`, ported `test-fixtures` (full claude-code bats set). | `b910cd4`, `e08da5d`, `a91d107`, `e42314f`, `08dc4a2`, + this commit | bats 307/307; `tsc --noEmit` clean; bash -n / node --check / PS parse all clean. |
| 3 (groundwork) | Persistent REPL daemon (`lib-sh/repl-daemon.js`) + `repl_invoke_persistent` wrapper: one repl child serves N requests, auto-start, crash-restart, concurrency, spawn-per-call fallback. | `a91d107`, `e42314f` | daemon bats 4/4; persistent-wrapper bats 4/4. |

Full McpServer solution suite on this branch: green except 5 pre-existing failures unrelated to this work (3 `SessionLogErrorTests` + 1 `PlanReview` + 1 `ToolRegistry` validation tests that hit a live `localhost:7147` server / external state; they fail identically on `main`).

## Canonical core design (see in-repo notes)

- `plugins/core/lib-node/README.md` - TS package provenance (base `cline-v2/src`, patches A-D), config surface, and what stays per-plugin host glue.
- `plugins/core/lib-ps/GAPS.md` - deliberately deferred PowerShell parity items.
- `plugins/core/README.md` - contract rules (plugins never edit synced files; host diffs live in `plugin-env`).
- Reconciliation source: codex `repl-invoke.sh` is the canonical base (strict superset); the shared UpsertTurnAsync patch was committed across all 5 shell repos this session (`97aab2d` claude-code, `f1bfae0`/`a934ba2`/`820bff8`/`ca9be45` cowork/codex/copilot/grok).

## REMAINING (not done - do not assume complete)

1. **Phase 2 fan-out** to the 8 plugin repos: run `plugins/core/sync/sync-plugin-core.sh <repo>` per repo, replace each `lib/` with synced core + a `plugin-env.sh` (values per `lib-sh/plugin-env.template.sh`), reduce hook scripts to `hooks-templates/wrapper.sh.template` wrappers, install `ci-templates/core-guard.yml`, run each repo's bats/jest. Pilot order: claude-code -> cowork -> grok -> copilot -> codex (shell), then opencode -> cline-v2 -> cline (npm core). Node plugins consume `@sharpninja/mcpserver-plugin-core`.
2. **Phase 3 completion**: enable persistent REPL by default in background-capable hosts; delete plugin-local lifecycle shims (`repl-invoke.ps1` no-ops, node `session-shim` state machine) once hosts call the stateless verbs; remove the deprecated `workflow.todo/requirements/memory` dispatcher namespaces (grep-gated: zero plugin references first); demote `cache/current-turn.yaml` to write-through cache.
3. **Merge** `feature/sessionlog-lifecycle` -> `main`, then deploy (`build.ps1 UpdateService`) so the live server gains Phases 1-3 (currently it has only Phase 0).
4. **lib-ps packaging decision**: whether codex/copilot receive `lib-ps`; replace their top-level `Invoke-{Host}McpPlugin.ps1` forks with the merged `Invoke-McpPlugin.ps1`.

## Notes

- The live `localhost:7147` McpServer service is running Phase 0 only (commit `37a3c78`). Phases 1-3 are branch-local and undeployed.
- node_modules / `.staged-plugin` / `dist` / `cache` under `plugins/core` are gitignored (`plugins/core/.gitignore`).
