# MCP Memory Workflow

MCP memories are durable operator guidance stored by McpServer and scoped by workspace. Treat the MCP memory store as the authoritative shared record for cross-agent continuity. Agent-local memory files may be used as private caches or migration sources, but they are not the shared source of truth.

## Scopes

- `Global` memories apply to every workspace and must contain only guidance the operator intends to share everywhere.
- `Workspace` memories apply only to the active workspace and must be stored with that workspace ownership.
- `Effective` listing returns `Global` memories first sorted by ID, then current `Workspace` memories sorted by ID.
- Workspace-scoped memories must not be copied, applied, or replayed into a different workspace unless the operator explicitly asks for that new memory to exist there.

## Tool Surfaces

Use the required plugin or MCP tool surface for normal work:

- MCP tools: `memory_add`, `memory_list`, `memory_update`, `memory_remove`
- REPL workflow: `workflow.memory.add`, `workflow.memory.list`, `workflow.memory.update`, `workflow.memory.remove`
- REST `/mcpserver/memory` only when explicitly allowed for non-plugin diagnostics

Every mutation should include `updatedBy` with the real agent or user identity when the surface supports it. Do not use placeholders or legacy aliases.

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
