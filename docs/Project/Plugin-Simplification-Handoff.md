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

1. **Phase 2 fan-out** to the 8 plugin repos. The migration MECHANICS are tooled and proven, but a fan-out attempt (2026-06-13) surfaced a blocking seam that must be fixed in the core FIRST:

   - **Pilot finding (claude-code)**: baseline `BATS_TEST_TIMEOUT=25 bats tests/ </dev/null` = 271/0. After `sync-plugin-core.sh --include-ps` + `generate-wrappers.sh claude-code`, the suite ran 268/271 with **0 failures but 3 tests that did not execute**: the 2 `user-prompt-submit` tests and `repl-invoke-shim.bats :: workflow.requirements.updateFr falls back...`. Root-caused to a **cache-dir seam in the canonical core**: `lib-sh/resolve-cache-dir.sh` returns the flat `<root>/cache`, while the v4 `lib-sh/cache-scope.sh` writes session-state under a workspace/session-scoped subpath. This aligns in the staged-fixture harness (`test-fixtures/core-staging.bash` builds `.staged-plugin`, 307/307 green) but NOT in a plugin repo's own `tests/` harness, where the hook resolves a different cache dir than `init_test_cache` wrote to → hook emits `"status":"no-session"` instead of `"turn-opened"`. Secondary: the ported fixtures moved the `CLAUDE_PLUGIN_ROOT`/`PLUGIN_ROOT_OVERRIDE` exports BEFORE `init_test_cache` (repo originals export them after); the canonical cache-scope reads `PLUGIN_ROOT_OVERRIDE` at `init_test_cache` time, so repo test files need that reorder.
   - **Fix-first**: reconcile `resolve-cache-dir.sh` and `cache-scope.sh` so a plugin repo's flat-cache hook reads the same session-state path the v4 scope writes (or make the hooks consistently use cache-scope). Re-prove against BOTH the staged fixtures AND a real repo's `tests/`.
   - **Then per repo**: `sync-plugin-core.sh <repo> [--include-ps for claude-code,grok]` -> `generate-wrappers.sh <host> <repo>` -> copy `ci-templates/core-guard.yml` to `<repo>/.github/workflows/` -> align the repo's shared-lib test files to the canonical fixtures (export ordering + canonical `cache-scope-helper.bash`) or replace them with the fixtures pointed at the repo root -> gate on `BATS_TEST_TIMEOUT=25 bats tests/ </dev/null` (0 fail AND executed-count == baseline; watch for the "Executed N instead of expected M" warning that signals silently-dropped tests). Pilot order: claude-code -> cowork -> grok -> copilot -> codex (shell), then opencode -> cline-v2 -> cline (npm core).
   - **bats flakiness note**: `cache-flush` replay tests intermittently hang without `BATS_TEST_TIMEOUT` (a replayed child blocks on stdin/network); always run with `BATS_TEST_TIMEOUT` set and `< /dev/null`. The 5 shell repos are currently CLEAN (the 2026-06-13 attempt was fully reverted; no half-migrated state).
   - **DECISION REQUIRED before fan-out — per-repo validation model.** The cache-dir determinism seam is now FIXED in the core (`f9eeebc`: `init_test_cache` pins `MCP_CACHE_DIR_OVERRIDE`; verified claude-code's 2 user-prompt-submit tests pass after re-sync, 29 cache fixtures stay green). But a second class of repo-test failures is **intended behavior change, not a bug**: the canonical lib adopts codex's typed-client-FIRST requirements routing (reconciliation "bugfix-keep"), so each repo's old `repl-invoke-shim.bats` test `updateFr falls back when workflow emits auth error` (asserts workflow-first) is now wrong by design. The canonical fixture already replaces it with `updateFr uses typed client even when workflow would emit auth error` plus richer audit/hydration tests. So the repos' shared-lib bats are SUPERSEDED by `plugins/core/test-fixtures`. Pick the model (recommend C):
     - A) keep + adapt each repo's shared-lib bats to the canonical behavior (high effort, duplicates the core fixtures).
     - B) replace each repo's shared-lib bats with the canonical fixtures (they run against `.staged-plugin`, coupling the repo test to a sibling core checkout).
     - **C) RECOMMENDED**: delete each repo's now-superseded shared-lib bats; validate per repo by (1) the `CORE-MANIFEST.yaml` sha256 checksum guard proving the repo's synced lib is byte-identical to the core, (2) the core's 307 fixtures proving lib behavior, (3) a thin per-repo hook smoke + the repo's host-specific suites (skills/manifest). This also removes the duplicated test LOC, matching the consolidation goal. It is an opinionated change to 8 repos, so it needs your sign-off.
   - **Node fan-out** is heavier: `@sharpninja/mcpserver-plugin-core` is not published; wire it via `npm link` or a `file:` dependency, then replace each repo's duplicated `src` modules with imports and keep only the host glue documented in `plugins/core/lib-node/README.md`. opencode's jest needs `NODE_OPTIONS=--experimental-vm-modules` (its `test` script already sets it); baselines this session: cline 62/62, cline-v2 75/75, opencode 243/243 (branch-coverage threshold 89.67% vs 90% is the only red, pre-existing).
2. **Phase 3 completion**: enable persistent REPL by default in background-capable hosts; delete plugin-local lifecycle shims (`repl-invoke.ps1` no-ops, node `session-shim` state machine) once hosts call the stateless verbs; remove the deprecated `workflow.todo/requirements/memory` dispatcher namespaces (grep-gated: zero plugin references first); demote `cache/current-turn.yaml` to write-through cache.
3. **Merge** `feature/sessionlog-lifecycle` -> `main`, then deploy (`build.ps1 UpdateService`) so the live server gains Phases 1-3 (currently it has only Phase 0).
4. **lib-ps packaging decision**: whether codex/copilot receive `lib-ps`; replace their top-level `Invoke-{Host}McpPlugin.ps1` forks with the merged `Invoke-McpPlugin.ps1`.

## Notes

- The live `localhost:7147` McpServer service is running Phase 0 only (commit `37a3c78`). Phases 1-3 are branch-local and undeployed.
- node_modules / `.staged-plugin` / `dist` / `cache` under `plugins/core` are gitignored (`plugins/core/.gitignore`).
