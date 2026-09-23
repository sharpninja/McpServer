# MCP Memory Workflow

MCP memories are durable operator guidance stored by McpServer and scoped by workspace. Treat the MCP memory store as the authoritative shared record for cross-agent continuity. Agent-local memory files may be used as private caches or migration sources, but they are not the shared source of truth.

## Scopes

- `Global` memories apply to every workspace and must contain only guidance the operator intends to share everywhere.
- Creating a `Global` memory succeeds when the active workspace is empty or is the configured default workspace. The stored row is `Scope=Global` and `WorkspaceId=null`. `memory_add` and `memory_remember` accept an empty, omitted, or configured default `workspacePath` for `Global` scope. On HTTP, only `POST /mcpserver/memory` and `POST /mcpserver/memory/remember` proceed without a resolved workspace; list, get, update, and remove still require one. Full auth (JWT or a full workspace API key) is required; a default read-only API key cannot write.
- `Workspace` memories apply only to the active workspace and must be stored with that workspace ownership. Workspace scope still requires a real workspace path.
- `Effective` listing returns `Global` memories first sorted by ID, then current `Workspace` memories sorted by ID.
- Workspace-scoped memories must not be copied, applied, or replayed into a different workspace unless the operator explicitly asks for that new memory to exist there.

## Tool Surfaces

Use the required plugin or MCP tool surface for normal work:

- MCP tools: `memory_add`, `memory_list`, `memory_update`, `memory_remove`
- Additive CQRS verbs: `memory_remember`, `memory_recall`, `memory_explore`, `memory_consolidate`, `memory_promote`, `memory_revert`
- REPL workflow: `workflow.memory.add`, `workflow.memory.list`, `workflow.memory.update`, `workflow.memory.remove`, `workflow.memory.remember`, `workflow.memory.recall`, `workflow.memory.explore`, `workflow.memory.consolidate`, `workflow.memory.promote`, `workflow.memory.revert`
- REST `/mcpserver/memory` only when explicitly allowed for non-plugin diagnostics

`Idempotency-Key` is not supported on memory write endpoints. A duplicate `memory_remember` (or other write) creates a second memory with a new id; callers must treat duplicate posts as duplicate rows.

Additive verbs (MCP-MEMORY-002):

- `memory_remember` persists a multi-layer memory. Injection later uses raw `Content` (or legacy `Text`) only. Title, summary, confidence, and tags are stored when provided but are never injected.
- `memory_recall` returns ranked Effective hits by meaning or keyword (`query`, optional `minScore`, `topN`, `tags`, `type`, `scope`). `MemoryClient.RecallAsync` and `workflow.memory.recall` return `MemoryRecallResult` with `items`, per-hit `score`, and `rankingMode`. They do not collapse a live recall body down to status code only.
- `memory_promote` copies an operator-selected `sessionlog` or `context` source (`sourceKind` + `sourceRef`) into memory. Do not promote unless the operator asks.
- `memory_consolidate` plans a sleep/merge. Default is dry-run; apply writes only when `dryRun` is false.
- `memory_revert` restores snapshot N and appends history.

`memory_explore` walks directed edges from a seed id or the top recall hit for a query seed. Results stay inside the caller Effective set. Soft-deleted seeds return 404; soft-deleted or foreign neighbors are omitted. Explore does not rewrite memory Content.

Hebbian co-retrieved edges are off by default (`Mcp:Memory:Hebbian:Enabled=false` / `MemoryExploreLimits.DefaultHebbianEnabled`). While Hebbian is off, explore never returns co-retrieved-only edges, even if those rows already exist. Enabling Hebbian can strengthen co-retrieved edges without changing the recall ranking path.

Every mutation should include `updatedBy` with the real agent or user identity when the surface supports it. Do not use placeholders or legacy aliases.

Memory writes are first-party mutations under FR-MCP-173. On `develop` and on the live Linux box MCP they persist without `ITurnTransactionCoordinator` or keyserver signing even when `Mcp:TurnTransactions:Enabled=true`. Keyserver remains QuadBrain/brain-slot only. Do not disable the live turn-transaction flag to make `memory_*` succeed.

## REQUIRED MEMORIES Injection

Supported plugins render the active Effective set at host-supported request boundaries. The production renderer (`MemoryRequiredMemoriesRenderer`) writes:

```
REQUIRED MEMORIES
- <raw Content or legacy Text>
```

An empty Effective set still renders:

```
REQUIRED MEMORIES
- None
```

Preserve raw memory text. Do not summarize, paraphrase, decorate, or add secrets. Summary, confidence, tags, and titles must not appear in the injected block.

All eight official plugins inject this block at host-supported request boundaries. Each plugin ships `skills/memory/SKILL.md` plus `memory-descriptor.json`, with always-on injection on the host path (`hooks/scripts/memory-context.ps1` and/or `src/memory-context.ts`). Canonical host payloads also live in this repo under `plugins/core/hosts/{claude-code,claude-cowork,cline,cline-v2,grok,copilot,codex,opencode}/`.

Grok remains the default CI bench lane. After H7a `agree:true` (`docs/benchmarks/h7a-value-gate.json`), `./build.ps1 BenchMemory -Plugin all` is unblocked. S7b/H7b is on develop (PR #50). See `docs/benchmarks/README.md` and `docs/benchmarks/results/memory-bench-multiturn-20260919T091800Z.md`.

## Importing Agent-Local Memories

Before importing existing agent-local memory content:

1. Inventory candidate records from the local store without writing them.
2. Filter candidates to the active workspace, or to truly global guidance the operator explicitly approves for `Global` scope.
3. Exclude secrets, credentials, private unrelated workspace notes, transient guesses, stale diagnostics, and content whose source workspace is unclear.
4. Preserve the raw guidance text exactly when it becomes a memory. Do not summarize or rewrite it during import.
5. Use `memory_add` or `workflow.memory.add` with the intended `scope`, `category`, optional explicit `MEMORY-*` ID, and accurate `updatedBy`.
6. Verify the imported records with `memory_list` or `workflow.memory.list scope: Effective`.

Bulk imports are not automatic. If provenance, scope, or operator approval is missing, leave the agent-local record local and do not import it.

## Fallbacks

Plugins may keep local failsafe records for memory mutations that could not be acknowledged by the MCP server. These records are replay aids only. After the MCP server acknowledges a memory mutation, the local failsafe entry must be removed.

Agent-local memory stores may cache visible MCP memories for resiliency, but agents must prefer the live MCP memory surface when it is trusted and available.

## Source And Audit Attribution

Memory mutations have two audit layers:

- Data audit: McpServer records memory row changes through storage auditing.
- Workflow audit: the active session log records the agent action that created, updated, or removed a memory.

For every successful memory mutation, append a session-log action through `workflow.sessionlog.appendActions` when a turn is active. Use action `type: edit`, `status: completed`, and a description that identifies the memory operation and memory ID when known.

When importing memory content from a local source, keep source attribution in the session-log action or dialog. Do not add private file paths, credentials, or unrelated personal details to the memory text itself unless the operator explicitly wants that text preserved as guidance.

## Operator UI

The first-party Memory UI is served at `/memory/` from packaged `wwwroot/memory` static assets. It lists only the active workspace Effective set, edits through the same CQRS REST update path (`PUT /mcpserver/memory/{id}`), and reverts in three actions (open versions, select, revert). Foreign ids fail closed. The ship path is Nuke `UpdateService`; do not run it unless the operator asks. Auth matches other `/mcpserver` pages (`X-Api-Key`). Content-Security / no inline-eval matches the Use Case Manager sibling UI. Missing hashed bundles and `/memory/unknown-asset` return 404, not a false 200 index.
