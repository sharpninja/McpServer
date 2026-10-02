# McpServer Plugin Core

Canonical shared infrastructure for every `mcpserver-*-plugin` repo. The
plugin repos carry only their host manifest, host-specific hook entry points,
skills, and a `CORE-MANIFEST.yaml`; all transport, marker-trust, cache, and
session-log logic lives here and is distributed by sync.

## Layout

- `lib-sh/` - canonical bash library (repl-invoke, marker-resolver, cache
  manager, memory context, JS helper shims). Parameterized per host via
  `plugin-env.sh` in each plugin repo.
- `lib-ps/` - PowerShell twins for hosts that run hooks under pwsh.
- `lib-node/` - source of the `@sharpninja/mcpserver-plugin-core` npm package
  consumed by the Node plugins (cline, cline-v2, opencode).
- `hooks-templates/` - reference hook wrappers (5-10 lines each) that source
  `lib/plugin-env.sh` + the shared lib and call one shared entry function.
- `test-fixtures/` - shared bats suites and golden REPL envelope fixtures,
  parameterized by explicit plugin roots plus `MCP_CACHE_DIR_OVERRIDE`, runnable against the core itself
  and against any synced plugin repo.
- `hosts/` - per-plugin memory skill/descriptor payloads (Grok plus S7b hosts).
  Apply with `sync/apply-memory-s7b-hosts.ps1 -PluginRoot <repo> -HostName <id>`.
- `sync/` - distribution tooling:
  - `sync-plugin-core.sh|ps1 <plugin-root> [--include-ps]` copies the libs
    into `<plugin>/lib/` and writes `CORE-MANIFEST.yaml` (core git version +
    per-file sha256).
  - `apply-memory-s7b-hosts.ps1 <plugin-root> -HostName <id>` copies the host
    memory skill, descriptor, and descriptor-load tests into a plugin repo.
  - `check-core-integrity.sh|ps1 <plugin-root>` is the CI guard: it fails the
    build when any synced file was edited locally. Fix in this directory and
    re-sync; never patch a plugin's copy.

## Contract rules

1. The REPL/REST wire contract is defined by this repository (McpServer); the
   core libs and their contract tests change atomically with the server in
   one PR.
2. Plugin repos never edit synced files. The checksum guard enforces this.
3. Host differences live in `plugin-env.sh` (agent name, plugin-root env var,
   hook payload field names), never in forked copies of shared logic.
4. New shared logic lands here first, with a bats/jest test in
   `test-fixtures/`, then fans out via sync.

## Session Mutation Receipts

`Invoke-ReplMethod`, the shared `Invoke-McpPlugin.ps1` wrapper, and the
host wrapper emit one YAML `type: result` envelope for each local
`workflow.sessionlog.*` mutation. `payload.result` contains `code`, `agent`,
`sessionId`, `requestId`, `method`, `persisted`, `degraded`, `queued`,
`retryable`, `failsafePath`, `message`, and `childStderr`. Session-only title
changes leave `requestId` empty. Diagnostic stderr is separate from stdout;
hook entrypoints capture the receipt and continue emitting their own hook JSON.

- `persisted`: typed remote confirmation, exit 0.
- `queued`: confirmed recovery file retained, exit 0; not primary persistence.
- `unchanged`: no remote write performed, exit 0; not a persistence claim.
- `rejected` or `lost`: operation not confirmed, nonzero exit.

Each invocation resets the previous outcome. Empty or identity-mismatched
title responses are rejected and cannot delete the recovery file. Both title
methods require `retitled: true`; the typed client preserves that field.
Supersession uses the same durability-first plan/TODO resolver as other verbs:
durable turns omit unbound links, while first persistence uses cached links
before falling back to exact `None`.
