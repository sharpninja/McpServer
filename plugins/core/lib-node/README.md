# @sharpninja/mcpserver-plugin-core

Canonical TypeScript core for the McpServer Node plugins (cline v1,
cline-v2, opencode). Base sources: `mcpserver-cline-v2-plugin/src` (per the
Phase 2 reconciliation report), with four patches and the opencode
test-facing helper exports applied:

- Patch A (`cache/cache-manager.ts`): cline v1's retry cap ported into the
  async flush (entries with `retryCount >= 3` are skipped; the incremented
  retryCount is persisted on failure), `failsafeDir()` host segment from
  `config.pluginId` (TR-MCP-AGENT-PARITY-013 workspace-scoped v4 layout
  kept), `cacheStatus()` re-exported for the v1 glue.
- Patch B (`tools/todo.ts`): `implementationTasks` items advertise
  `oneOf [string | {task,done}]` matching the normalizer (v1 behavior);
  `internalTodoCacheDir()` segment from `config.pluginId`;
  `MCPSERVER_INTERNAL_TODO` read first with the codex-prefixed names as
  back-compat aliases. `todo_internal_*` tools kept.
- Patch C (`tools/requirements.ts`): the shared batch schema keeps v2's
  `oneOf(array | string)` top shape (matches `parseRecordsValue`) and
  reinstates v1's per-record item schemas inside the array branch.
- Patch D (`tools/schema-validation.ts`): ported from cline v1 as a pure
  JSON-schema validator (the MCP SDK type import was replaced with a
  structural interface); wired as the opt-in `config.validateArguments`
  pre-dispatch step in `HostContext.dispatchTool`.
- `transport/repl-bridge.ts`: YAML + `---` framing kept (the only framing
  `AgentStdioProtocol.cs` dispatches); `MCPSERVER_REPL_COMMAND` /
  `MCPSERVER_REPL_ARGS` spawn overrides added; opencode's dead module-level
  `slug()` was never copied.
- `runtime/host-context.ts` (new): the shared plugin.ts logic - `utcStamp`,
  `slug`, `asRecord`, `stringValue`, `contextWorkspacePath`,
  `contextPrompt`, `contextModel`, `toolName`, `toolInput`, `toolError`,
  `setMarkerEnvironment`, `dispatchTool` routing, and the
  startSession/completeSession/appendToolAction choreography, with
  `agentName` / `pluginId` threaded from the config instead of hardcoded
  Cline/OpenCode strings.
- `runtime/core-config.ts` (new): process-wide config consumed by the
  modules above; `createMcpServerPluginCore(config)` in `index.ts` is the
  factory.

Config surface: `{ agentName, pluginId, sessionTitle, workspacePath,
bridge, autoBootstrap, autoFlushCache, toolTimeoutMs, validateArguments,
replCommand }`.

## What stays per-plugin (host glue, NOT in this package)

- cline v1: `src/index.ts` (MCP SDK `Server`/`StdioServerTransport` wiring,
  the `{content:[{type:'text'}]}` envelope wrap, repl auto-install - the
  POSIX-only `execSync('which mcpserver-repl')` + bash path should move
  behind a host hook at fan-out) and `src/tools/plugin-helpers.ts`
  (`mcp_cline_status` / `final_response`; consumes the core's
  `getSessionShimState` and `cacheStatus` re-exports).
- cline-v2: `src/plugin.ts` (`@cline/core` `createTool`/`registerTool`,
  beforeRun/beforeTool/afterTool/afterRun/onEvent hooks, `contextModel()`
  wiring, `logger()`).
- opencode: `src/plugin.ts` (zod `jsonPropToZod`/`jsonSchemaToZodShape`,
  `wrapResult`, event-name regex mapping, Hooks tool map) and
  `src/plugin-api.ts` (structural `@opencode-ai/plugin` typings). At
  fan-out, opencode regains the v4 workspace-scoped failsafe by consuming
  the core cache-manager (its local copy had dropped it).

## Breaking changes

### 0.2.0 (QuadBrain removal)

Three public exports were deleted from `src/index.ts`. Any consumer that
imported them will fail to compile against 0.2.0 and must drop the import:

- `brainSlotTools`
- `canHandleBrainSlotTool`
- `handleBrainSlotTool`

Rationale: nothing about QuadBrain is exposed to the agent plugins at all.
Not gated, not identity-filtered: absent. QuadBrain remains reachable only as
the OpenAI-compatible model endpoint that QBAgent calls directly, so the
shared plugin core carries no brain-slot tool descriptors, no dispatch
branches, and no public re-exports. `HostContext.dispatchTool` now treats
every `brain_slot*` name as an unknown tool. Coverage lives in
`tests/quadbrain-absence.test.ts`; the version floor is asserted in
`tests/package-version.test.ts`.

Under semver a breaking change on a `0.x` line bumps the minor, which is why
this is 0.2.0 rather than 0.1.1 or 1.0.0.

## Validation

`npm install && npx tsc --noEmit` passes. Jest suites run at fan-out time
in the consuming plugin repos.
