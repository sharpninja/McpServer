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
- `sync/` - distribution tooling:
  - `sync-plugin-core.sh|ps1 <plugin-root> [--include-ps]` copies the libs
    into `<plugin>/lib/` and writes `CORE-MANIFEST.yaml` (core git version +
    per-file sha256).
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
