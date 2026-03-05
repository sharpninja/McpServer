# McpServer — Website Copy (Canva)

Each section maps to a Canva website block.

---

## Section 1 — Hero

**Headline:** Your AI Agents, Finally Coordinated.

**Subheadline:** McpServer is a self-hosted context server that gives AI coding agents persistent memory, shared workspace state, and structured task management — using the open Model Context Protocol standard.

**CTA Button:** Get Started on GitHub → https://github.com/sharpninja/McpServer
**Secondary link:** View Documentation

**Visual:** ![Architecture Diagram](diagrams/architecture.png)

---

## Section 2 — Problem

**Label:** THE PROBLEM
**Headline:** AI Agents Are Powerful. But Stateless.
**Body:** Every session starts fresh. Your agents forget last session's decisions, don't know what other agents are working on, and fill in gaps with training data instead of your actual code. The result: repeated work, contradictory decisions, and no audit trail.

**Card 1 — Lost Context:** Design decisions made in one session disappear in the next. Agents re-research. Consistency breaks.
**Card 2 — No Coordination:** Multiple agents. Multiple tools. No shared state. Each agent is an isolated silo.
**Card 3 — Hallucinated Context:** Without access to your workspace, agents guess from training data — not from your code.

---

## Section 3 — Solution

**Label:** THE SOLUTION
**Headline:** One Server. All Agents. One Truth.
**Body:** McpServer runs locally alongside your codebase. It gives every connected agent the same view: your todos, your session history, your code — semantically searchable, always current. Agents coordinate through McpServer instead of working in isolation. Every decision is logged. Every action is audited. Context is retrieved, not hallucinated.

**Feature map:** ![Features Overview](diagrams/features.png)

**Column 1 — Persistent Memory:** Session logs capture every decision, action, and design choice. Agents resume with full context.
**Column 2 — Shared Workspace State:** All agents read from and write to the same TODO store, context index, and session log.
**Column 3 — Grounded Context:** Semantic search over your actual code and docs — not training data guesses.

---

## Section 4 — Features

**Label:** KEY FEATURES
**Headline:** Everything your agents need. Nothing they don't.

**Card 1 — Hybrid Context Search:** FTS5 full-text + HNSW vector search (all-MiniLM-L6-v2), fused with BM25 scoring. Runs entirely locally. No cloud API.
**Card 2 — TODO Management:** Full CRUD todo API with YAML or SQLite backend. Agents query, create, and complete tasks programmatically.
**Card 3 — Session Logging:** Every agent interaction captured — queries, decisions, commits, files modified. Full audit trail, searchable.
**Card 4 — GitHub Issue Sync:** Bidirectional sync between workspace TODOs and GitHub Issues. Same API for local tasks and GitHub.
**Card 5 — GraphRAG:** Graph-augmented retrieval for deep codebase reasoning. Local, optional, per-workspace. Falls back gracefully.
**Card 6 — Multi-Workspace:** One server. Many projects. Isolated context, todos, and keys per workspace. Routing via HTTP header.

---

## Section 5 — UI Tooling

**Label:** UI TOOLING
**Headline:** The right interface for every workflow.

**UI Tooling Diagram:** ![UI Tooling](diagrams/ui-tooling.png)

**Tool 1 — Blazor Web UI:** Full browser-based dashboard. Todos, sessions, agents, templates, context search. GitHub Primer CSS.
**Tool 2 — Director CLI:** `dotnet tool install --global SharpNinja.McpServer.Director` — https://www.nuget.org/packages/SharpNinja.McpServer.Director
**Tool 3 — Director TUI:** `director ui` — full terminal UI. Role-filtered tabs, keyboard navigation, auto-refresh. Works over SSH.
**Tool 4 — VS / VS Code Extension:** VSIX extension. Browse and update todos without leaving your editor.
**Tool 5 — Client NuGet:** `dotnet add package SharpNinja.McpServer.Client` — https://www.nuget.org/packages/SharpNinja.McpServer.Client
**Tool 6 — MCP STDIO / HTTP:** Native MCP transport. Connect GitHub Copilot, Cursor, Codex, or Claude with zero custom code.

---

## Section 6 — Architecture

**Label:** HOW IT WORKS
**Headline:** Simple by design. Powerful at scale.

**Diagram:** ![Architecture](diagrams/architecture.png)
**Caption:** McpServer sits between your AI agents and your workspace. Agents read from it and write to it. Your workspace stays as the source of truth.

---

## Section 7 — Quick Start

**Label:** GET STARTED
**Headline:** Running in under two minutes.

Step 1: `dotnet restore McpServer.sln && dotnet build McpServer.sln -c Staging`
Step 2: `.\scripts\Start-McpServer.ps1 -Configuration Staging`
Step 3: Open `http://localhost:7147/swagger`

Or install Director: `dotnet tool install --global SharpNinja.McpServer.Director && director health`

**CTA:** View Full Documentation → https://github.com/sharpninja/McpServer

---

## Section 8 — Footer

**Tagline:** McpServer — Context intelligence for AI-assisted development.

- GitHub Repository → https://github.com/sharpninja/McpServer
- NuGet: Client → https://www.nuget.org/packages/SharpNinja.McpServer.Client
- NuGet: Director → https://www.nuget.org/packages/SharpNinja.McpServer.Director
- Swagger UI → http://localhost:7147/swagger
- MCP Spec → https://modelcontextprotocol.io

**License:** MIT · © SharpNinja
