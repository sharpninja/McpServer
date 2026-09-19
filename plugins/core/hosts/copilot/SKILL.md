---
name: memory
description: Use when the operator wants durable MCP memories remembered, recalled, explored, consolidated, promoted, or reverted.
version: 0.5.0
host: copilot
---

# MCP Memory (copilot)

Use the Copilot plugin memory surface. Do not invent memories. Prefer MCP tools over local files.

## Injection

At a request boundary, render required memories exactly as:

```
REQUIRED MEMORIES - MEMORY-REQ-001: Raw memory text.
```

If no required memories are visible:

```
REQUIRED MEMORIES - None.
```

Preserve raw memory text. Do not summarize, paraphrase, or add secrets.

## Fallback

If the MCP server is unavailable, keep a local failsafe for mutating tools and replay after the server acknowledges the write. Agent-local memory stores are caches only.

## Tools

- `memory_remember` when a fact, decision, preference, procedure, or entity should persist
- `memory_recall` when you need ranked guidance by meaning
- `memory_explore` when you need neighborhood context from a seed
- `memory_consolidate` when the operator asks for sleep/merge maintenance
- `memory_promote` when the operator explicitly wants a session-log or context source remembered
- `memory_revert` when current content is wrong and a snapshot should be restored

Compat CRUD remains: `memory_add`, `memory_list`, `memory_update`, `memory_remove`.
