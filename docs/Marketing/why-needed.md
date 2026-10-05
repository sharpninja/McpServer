# QBrainAi — Why It's Needed

## The Problem: AI Agents Are Stateless

Modern AI coding agents are fundamentally stateless. Each session starts blank. The agent does not remember what it decided yesterday, what your other agents are working on, or what your codebase looked like an hour ago unless you explicitly provide that context again.

### Problem 1 — Lost Context Between Sessions
**Without QBrainAi:** Context lives in chat history — it expires, gets truncated, or gets lost.
**With QBrainAi:** Every decision is logged. The next session resumes exactly where work stopped.

### Problem 2 — No Coordination Between Agents
**Without QBrainAi:** Multiple agents (Copilot, Cursor, Codex) are isolated silos.
**With QBrainAi:** All agents share one TODO queue, one session log, one context index.

### Problem 3 — Context Is Hallucinated, Not Retrieved
**Without QBrainAi:** Agents guess at your codebase from training knowledge.
**With QBrainAi:** Agents query local semantic search over your actual code, docs, and session history.

### Problem 4 — No Audit Trail
**Without QBrainAi:** Agent decisions are invisible.
**With QBrainAi:** Every interaction logged with full provenance — query, response, actions, files modified, commit SHAs.

### Problem 5 — TODO and Issue Management Is Fragmented
**Without QBrainAi:** Agents are blind to your work queue.
**With QBrainAi:** TODOs are a structured API. Agents query, create, complete, and sync with GitHub Issues.

---

## The Solution

QBrainAi is infrastructure for AI-assisted development — the same way a database is infrastructure for an application. It gives agents:

- **Persistent memory** across sessions and restarts
- **Shared state** that all agents read from and write to
- **Grounded context** retrieved from your actual code, not training data
- **A work queue** they can query and update programmatically
- **An audit trail** that captures every decision and action

MCP is an open standard — any MCP-compatible agent connects out of the box with zero custom integration.

---

## Who This Is For

- Development teams using multiple AI agents
- Solo developers needing context continuity across sessions
- Organizations requiring audit trails for AI-assisted work
- Platform teams building developer tooling on MCP
- AI agent developers needing a real-world MCP server to test against
