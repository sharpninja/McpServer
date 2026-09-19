# give-yourself-memory

One-prompt sample that teaches an agent to persist and retrieve operator guidance through MCP memory tools.

Use these real tool names. Do not invent aliases.

1. `memory_remember` a durable fact, decision, preference, procedure, or entity when the operator wants it to persist.
2. `memory_recall` by meaning when you need ranked guidance for the current question.
3. `memory_explore` from a seed id or query when you need neighborhood context.
4. `memory_consolidate` only when the operator asks for sleep/merge maintenance. Default is dry-run.
5. `memory_promote` a session-log or context source only when the operator explicitly wants that text remembered.
6. `memory_revert` a memory to a prior snapshot when the current content is wrong.

Compat CRUD remains available: `memory_add`, `memory_list`, `memory_update`, `memory_remove`.

REPL mirrors: `workflow.memory.remember`, `workflow.memory.recall`, `workflow.memory.explore`, `workflow.memory.consolidate`, `workflow.memory.promote`, `workflow.memory.revert`.

REST mirrors: `POST /mcpserver/memory/remember`, `POST /mcpserver/memory/recall`, `POST /mcpserver/memory/explore`, `POST /mcpserver/memory/consolidate`, `POST /mcpserver/memory/promote`, `GET /mcpserver/memory/{id}/versions`, `POST /mcpserver/memory/{id}/revert`.
