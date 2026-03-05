# McpServer — Presentation Outline (Canva)

12-slide deck for technical audiences. Each slide includes layout, full copy, and speaker notes.

---

## Slide 1 — Title
**Layout:** Full-bleed dark background, centered.
**Title:** McpServer
**Subtitle:** Context Intelligence for AI Coding Agents
**Tagline:** Persistent memory. Shared workspace state. Open standard.
**Links:** GitHub: https://github.com/sharpninja/McpServer | NuGet: https://www.nuget.org/packages/SharpNinja.McpServer.Client
**Speaker Notes:** McpServer is a self-hosted MCP context server. Today I will cover what it does, why it exists, and how agents connect to it. This is technical infrastructure — the database layer for AI-assisted development.

---

## Slide 2 — The Problem
**Layout:** Split — left: bullets, right: isolated-agent illustration.
**Title:** AI Agents Are Stateless
**Bullets:**
- Every session starts blank — no memory of past decisions
- Multiple agents working in parallel have no shared state
- Agents fill context gaps with training data, not your actual code
- No audit trail — no record of what agents did or why
**Speaker Notes:** The fundamental problem with today's AI coding agents is statefulness. They are incredibly capable within a session, but they forget everything when you close the window. If you run multiple agents — Copilot in VS Code, Cursor in the terminal, Codex running tasks — they have no awareness of each other. McpServer fixes this.

---

## Slide 3 — The Solution
**Layout:** Headline at top, three columns, feature map below.
**Title:** McpServer: One Server. All Agents. One Truth.
**Column 1 — Persistent Memory:** Session logs capture every decision, action, and design choice. Agents resume with full context.
**Column 2 — Shared Workspace State:** All agents read from and write to the same TODO store, context index, and session log.
**Column 3 — Grounded Context:** Semantic search over your actual code and docs — not training data guesses.
**Feature Map:** ![Features Overview](diagrams/features.png)
**Speaker Notes:** McpServer is infrastructure, not an agent. It doesn't replace Copilot or Cursor — it makes them dramatically more effective. Think of it the same way you think of a database: your application doesn't work without it, but it's not the application itself.

---

## Slide 4 — Architecture
**Layout:** Centered diagram on light background.
**Title:** How It Works
**Diagram:** ![Architecture](diagrams/architecture.png)
**Caption:** McpServer is protocol-native. Agents connect over the open Model Context Protocol — no custom integration code required.
**Speaker Notes:** McpServer implements MCP — the Model Context Protocol, an open standard for AI agent tool-calling. Any MCP-compatible agent connects out of the box. It also exposes a full REST API for direct integration from scripts, CI pipelines, or .NET applications.

---

## Slide 5 — Feature: Hybrid Context Search
**Layout:** Left: description + bullets; right: code/JSON mockup.
**Title:** Hybrid Context Search
**Body:** Local FTS5 full-text + HNSW vector similarity (all-MiniLM-L6-v2 ONNX), fused with BM25 scoring.
**Bullets:**
- Zero cloud dependency — all embeddings run on-device
- Keyword and semantic search combined
- Workspace-scoped — only your code, your docs
- Endpoint: GET /mcpserver/context/search
**Speaker Notes:** The context search is the core feature. When an agent asks "what does this interface do?" McpServer searches your actual codebase semantically. It runs 384-dimension vector embeddings locally using an ONNX model — no API key, no cloud, no data leaving your machine.

---

## Slide 6 — Feature: TODO Management & Session Logging
**Layout:** Two panels side by side.
**Title:** Coordinated Work & Full Audit Trail
**Panel 1 — TODO Management:** CRUD API for structured TODO items. YAML or SQLite backend. Agents create, query, complete items. Bidirectional GitHub Issue sync. Endpoint: GET/POST/PUT/DELETE /mcpserver/todo
**Panel 2 — Session Logging:** Every AI agent interaction captured. Query, response, decisions, actions, files modified, commit SHAs. Searchable. Attributed per agent. Endpoint: POST /mcpserver/sessionlog
**Speaker Notes:** These two features solve the coordination and auditability problems. Agents share a single TODO queue. The session log is a full audit trail — useful for code review, compliance, and resuming interrupted work.

---

## Slide 7 — Feature: GitHub Issue Sync
**Layout:** Centered, full-width.
**Title:** Your TODO List and GitHub Issues — In Sync
**Body:** McpServer syncs workspace TODO items bidirectionally with GitHub Issues.
**Bullets:**
- Create GitHub Issues from TODO items automatically
- Close issues when TODOs are marked done
- Map ISSUE-* TODO IDs to GitHub issue numbers
- Filter by labels, state, and milestone
**Link:** https://github.com/sharpninja/McpServer
**Speaker Notes:** For teams using GitHub Issues for project tracking, agents can interact with the issue tracker through the same TODO API they already use. Stakeholders see progress in GitHub. Agents don't need to know GitHub's API separately.

---

## Slide 8 — Feature: GraphRAG
**Layout:** Dark background, graph visualization.
**Title:** GraphRAG — Deep Codebase Reasoning
**Body:** Optional graph-augmented retrieval that builds a knowledge graph over your codebase.
**Bullets:**
- local mode: narrow, precise queries
- global mode: broad architectural summaries
- drift mode: change-aware queries
- Falls back gracefully to hybrid search when unavailable
**Note:** Per-workspace · Optional · Zero cloud dependency
**Speaker Notes:** For complex architectural questions — "what components depend on this interface?" — GraphRAG provides graph-structured reasoning over the codebase. It's optional, per-workspace, and degrades gracefully.

---

## Slide 9 — UI Tooling
**Layout:** 2x3 grid of tool cards on dark background.
**Title:** The Right Interface for Every Workflow
**Diagram:** ![UI Tooling](diagrams/ui-tooling.png)
**Card 1 — Web UI:** Blazor dashboard. Todos, sessions, agents, templates, context search. GitHub Primer CSS.
**Card 2 — Director CLI:** dotnet tool install --global SharpNinja.McpServer.Director · https://www.nuget.org/packages/SharpNinja.McpServer.Director
**Card 3 — Director TUI:** director ui — full terminal UI, role-filtered tabs, auto-refresh.
**Card 4 — VS/VS Code Extension:** VSIX. Browse and update todos inside your editor. No context switching.
**Card 5 — Client NuGet:** SharpNinja.McpServer.Client · https://www.nuget.org/packages/SharpNinja.McpServer.Client
**Card 6 — MCP STDIO/HTTP:** Connect Copilot, Cursor, Codex, Claude. Endpoint: POST http://localhost:7147/mcp-transport
**Speaker Notes:** We built six access surfaces because different people work differently. The CLI is for automation. The TUI is for SSH sessions. The Web UI is for exploration and team review. The VSIX is for developers who don't want to leave VS Code. The NuGet client is for building on top of McpServer. The MCP transport is for agents.

---

## Slide 10 — Deployment
**Layout:** Two columns.
**Title:** Deploy Anywhere
**Options:**
- Windows Service — scripts\Update-McpService.ps1
- Docker — docker-compose -f docker-compose.mcp.yml up
- MSIX — scripts\Package-McpServerMsix.ps1
- Self-hosted — any .NET-capable host
- Multi-workspace — one process, unlimited projects
**Speaker Notes:** McpServer is designed to fit into whatever deployment model you already use. Local developer machines, CI/CD containers, enterprise fleet management via MSIX — the same binary, the same config model, the same API surface.

---

## Slide 11 — Getting Started
**Layout:** Numbered steps with code blocks.
**Title:** Running in Two Minutes
**Step 1 — Build:** dotnet restore McpServer.sln && dotnet build McpServer.sln -c Staging
**Step 2 — Run:** .\scripts\Start-McpServer.ps1 -Configuration Staging
**Step 3 — Connect:** http://localhost:7147/swagger
**Step 4 — Add to agent:** { "mcpServers": { "mcpserver": { "url": "http://localhost:7147/mcp-transport" } } }
**Agent Workflow:** ![Agent Workflow Sequence](diagrams/agent-workflow.png)
**CTA:** View Full Docs → https://github.com/sharpninja/McpServer
**Speaker Notes:** Four steps from zero to a running context server that any MCP agent can connect to. AGENTS-README-FIRST.yaml is written to your workspace root automatically — agents read it to get the API key, base URL, and connection prompt.

---

## Slide 12 — Call to Action
**Layout:** Full-bleed dark background, centered.
**Title:** Give Your Agents Memory.
**Body:** McpServer is open source, self-hosted, and ready to run.
**CTA 1:** Star on GitHub → https://github.com/sharpninja/McpServer
**CTA 2:** Install Director → dotnet tool install --global SharpNinja.McpServer.Director · https://www.nuget.org/packages/SharpNinja.McpServer.Director
**CTA 3:** Add the Client → dotnet add package SharpNinja.McpServer.Client · https://www.nuget.org/packages/SharpNinja.McpServer.Client
**Footer:** MIT · © SharpNinja · https://github.com/sharpninja/McpServer
**Speaker Notes:** The repository is public. The NuGet packages are published. The documentation covers configuration, deployment, and API reference. If you're building with AI coding agents, give them a context server. Start here.
