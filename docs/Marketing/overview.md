# McpServer — Overview

## One-Liner

**McpServer is a self-hosted context server that gives AI coding agents persistent memory, shared workspace state, and coordinated task management — using the open Model Context Protocol (MCP) standard.**

## Elevator Pitch

AI coding agents like GitHub Copilot, Cursor, Codex, and Claude are powerful but stateless. They forget what they did last session. They do not know what your teammates' agents decided yesterday. They cannot see your project's TODO list, session history, or design decisions unless you paste them in manually — every time.

McpServer solves this. It runs locally alongside your codebase and acts as the single source of truth for your workspace. Agents connect to it over HTTP or MCP STDIO and can search your code semantically, read and update TODO items, query session logs, sync with GitHub Issues, and retrieve structured requirements — all without leaving their natural tool-calling workflow.

The result: your agents coordinate instead of repeat. Context is deterministic, not hallucinated. Every decision is audited.

## What It Is

McpServer is a standalone **ASP.NET Core 9** application implementing both HTTP REST and the **Model Context Protocol (MCP)** wire format. It runs as a background service on a developer workstation or CI server, making workspace context available to any MCP-capable agent or IDE extension.

- **Self-hosted** — no cloud dependency; data stays in your environment.
- **Multi-tenant** — one process, multiple workspaces; routing via `X-Workspace-Path` header or API key.
- **Protocol-native** — built on the [Model Context Protocol](https://modelcontextprotocol.io/) open standard.
- **Open source** — https://github.com/sharpninja/McpServer

## Key Value Proposition

| Without McpServer | With McpServer |
|---|---|
| Agents start fresh every session | Agents resume with full context |
| Each agent has its own private state | All agents share one workspace truth |
| Decisions are lost in chat history | Every decision is logged and searchable |
| TODOs live in text files or issues | TODOs are API-queryable from any agent |
| No audit trail | Full session log with agent attribution |
| Vector search requires cloud API | Local 384-dim ONNX embeddings, no API key |
