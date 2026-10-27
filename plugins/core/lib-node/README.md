# @qbrainai/qbrain-ai-plugin-core

Canonical TypeScript core for the QBrainAi Node plugins (cline v1,
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
  modules above; `createQBrainAiPluginCore(config)` in `index.ts` is the
  factory.

Config surface: `{ agentName, pluginId, sessionTitle, workspacePath,
bridge, autoBootstrap, autoFlushCache, toolTimeoutMs, validateArguments,
replCommand }`.

## What stays per-plugin (host glue, NOT in this package)

- cline v1: `src/index.ts` (MCP SDK `Server`/`StdioServerTransport` wiring,
  the `{content:[{type:'text'}]}` envelope wrap, repl auto-install - the
  POSIX-only `execSync('which qbrain-ai-repl')` + bash path should move
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

## Governed audit aliases (C2)

`AuditOutbox` journals audit events under an explicit workspace, agent and MCP
session. Alias-enabled hosts must also pass the immutable `hostSessionKey`;
that key separates host-session storage even when MCP-session IDs coincide.
Never take any of these values from model tool arguments or ambient process
variables.

Use `AuditAliasConsumer.open(outbox, boundAliasSource, stableConsumerId)` before
accepting ingress or requesting its first mint. `boundAliasSource` implements
the A8 `registerAliasConsumer`, `listUnacknowledgedAliases`, `getAlias`,
`acknowledgeAlias`, and `onAliasCommitted` contract. The consumer subscribes and
checks durable enumeration at startup, before every enqueue and before every
flush. A callback is only a wakeup. Route host audit writes through the consumer,
not directly through the raw outbox, so a fresh canonical ID is resolved before
its first order is allocated. A8's journal implementation is a separate
integration prerequisite; this adapter does not implement minting.

The outbox copies a committed alias to `applied-aliases.yaml` before rewriting
queued event request IDs. This is the outbox's application record, not a second
authoritative mint journal. Filenames and sequence keys remain tied to the
original pending ID; either ID addresses that logical turn. Rebinding preserves
event ID, order, payload JSON and digest. Every locked operation reconciles an
interrupted application before proceeding. Unminted events remain pending and
are never submitted. Conflicting mappings, foreign contexts, attempted pending
events and independently allocated canonical sequences fail closed.

Acknowledgment follows durable application. A crash before acknowledgment is
safe to replay; the local application record remains after A8 prunes its alias.
A crash leaving an incomplete atomic-write temporary preserves that evidence in
quarantine. Recovery must resolve the quarantine explicitly; an alias ACK does
not prove the audit queue is clear. Dead process locks require
`recoverAbandonedLock()`; live or ambiguous locks are never displaced.

`consumer.status.ready` describes reconciliation only and stays false while
operations are queued/running, after a failure, or after close. Completion must
also inspect the full flush result and host-retained lost-write failures:
pending, rejected, quarantined or lost evidence blocks completion. Catching a
consumer exception or disabling a legacy cache flush is not audit clearance.
`close()` unsubscribes, refuses new/queued operations and waits for active work.
## Validation

Use `bun install`, `bun run build` and `bun run test --runInBand` in this directory. The local Jest suites include process-crash, context-isolation and alias-consumer contract checks. Fan-out integration must also run each consuming plugin contract suite.

## Required bundled-agent contract bootstrap

A governed product host can call `fullBootstrap` with `expectedWorkspacePath`,
its own proxy-aware `fetch`, and `requiredAgentPlugin` containing the exact
`agentName`, `pluginName`, `pluginVersion`, and `artifactSha256` from its reviewed bundled artifact pin.
For cline-qb these identities are `ClineQb` and
`@sharpninja/mcpserver-cline-v2-plugin`; the version is the reviewed bundle pin,
not a value selected from the marker or client environment.

Bootstrap reads one marker snapshot, verifies its signature, recomputes the
complete signed `agent_plugins` digest, and checks the required policy and exact
selected identity before any network request. Missing/mismatched contracts,
malformed YAML, foreign workspaces, and a marker changed during the nonce check
fail with `MCP_UNTRUSTED`. Successful results carry frozen
`agentPluginContract` metadata; no contract instructions or startup commands
are executed. Callers omitting the contract option retain the legacy bootstrap
interface.

The server advertises the ClineQb profile only when its explicit
`CLINE_QB_PLUGIN_ROOT` points to an absolute directory whose `package.json`
names the V2 package and supplies a valid version. It does not infer the version
from another installed plugin or the legacy synced fallback. This environment
variable configures the server's marker issuance only; a governed client must
use its pinned bundle and must not use it to discover executable code.

Contract verification is a startup prerequisite. It does not establish a
completed governed runtime or authenticate a hub peer; host factory wiring,
bundle integrity verification, and hub identity/digest enforcement are separate
integration obligations.

Use `verifyPinnedPluginArtifact({artifactPath, binding: reviewedPin})` before
extraction or execution. It requires an absolute regular archive file, a valid
SHA-256 review pin, and a positive bounded byte size (64 MiB by default, at most
512 MiB when explicitly selected by the host). Missing, empty, linked, oversized,
or mismatched artifacts fail closed. The result contains frozen binding metadata
and `readBytes()`, which returns a fresh copy of the exact bytes that passed the
hash check. Extract those bytes; reopening the path would discard this protection.
Pass the returned binding to `fullBootstrap` to compare it with the signed server
contract. The archive is not executed by either verifier. Already-extracted file
validation, safe extraction and mandatory product factory integration remain
separate obligations; this API alone does not certify an installation.

The server also requires `CLINE_QB_PLUGIN_ARTIFACT_SHA256`, configured from the
reviewed built archive's SHA-256. It publishes `artifact_sha256` in the ClineQb
profile and includes it in the contract digest. Existing profiles without that
field retain their original digest bytes. The three digests are distinct: archive
SHA-256 identifies built bytes; contract digest authenticates the whole agent
profile map; workspace verification-policy digest identifies verification rules.
