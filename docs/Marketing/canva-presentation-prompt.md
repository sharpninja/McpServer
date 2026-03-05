# Canva AI — Presentation Generation Prompt

Paste the prompt below into Canva's AI presentation generator (**Magic Design → Presentation**, or the AI presentation prompt field). The full 12-slide structure, all copy, speaker notes, and links are included.

---

## Prompt

```
Create a 12-slide professional presentation for a developer tool called McpServer. 
Audience: software engineers and technical leads evaluating AI-assisted development tooling.
Style: dark, technical, GitHub-inspired. Deep navy (#0d1117) or near-black slide backgrounds. White headlines. GitHub blue (#0969da) accent. Clean sans-serif font (Inter or equivalent). No emojis. No gradients. Minimal, precise, confident tone — like a Vercel or Cloudflare engineering deck.

---

SLIDE 1 — TITLE SLIDE
Layout: full-bleed dark background, centered content

Title: McpServer
Subtitle: Context Intelligence for AI Coding Agents
Tagline (smaller text below): Persistent memory. Shared workspace state. Open standard.
Bottom-left: GitHub: https://github.com/sharpninja/McpServer
Bottom-right: NuGet: https://www.nuget.org/packages/SharpNinja.McpServer.Client

Visual: abstract network/graph illustration — nodes connected by glowing lines on dark background, suggesting distributed agents connecting to a central hub.

Speaker notes: McpServer is a self-hosted MCP context server. Today I'll cover what it does, why it exists, and how agents connect to it. This is technical infrastructure — the database layer for AI-assisted development.

---

SLIDE 2 — THE PROBLEM
Layout: split — left 60% text, right 40% illustration

Title: AI Agents Are Stateless

Bullet points (large, one per line):
• Every session starts blank — no memory of past decisions
• Multiple agents working in parallel have no shared state
• Agents fill context gaps with training data, not your actual code
• No audit trail — no record of what agents did or why

Right visual: three separate boxes labeled "Copilot", "Cursor", "Codex" with NO connections between them — isolated silos with an X or broken-link icon between each.

Speaker notes: The fundamental problem with today's AI coding agents is statefulness. They are incredibly capable within a session, but they forget everything when you close the window. If you run multiple agents — Copilot in VS Code, Cursor in the terminal, Codex running tasks — they have no awareness of each other. McpServer fixes this.

---

SLIDE 3 — THE SOLUTION
Layout: headline at top, three equal columns below

Title: McpServer: One Server. All Agents. One Truth.

Three columns:

Column 1:
Icon: database or memory chip
Heading: Persistent Memory
Text: Session logs capture every decision, action, and design choice. Agents resume with full context.

Column 2:
Icon: connected nodes / sync
Heading: Shared Workspace State
Text: All agents read from and write to the same TODO store, context index, and session log.

Column 3:
Icon: magnifying glass on code
Heading: Grounded Context
Text: Semantic search over your actual code and docs — not training data guesses.

Bottom visual: clean dashboard mockup (dark-themed admin panel with sidebar and data grid).

Speaker notes: McpServer is infrastructure, not an agent. It doesn't replace Copilot or Cursor — it makes them dramatically more effective. Think of it the same way you think of a database: your application doesn't work without it, but it's not the application itself.

---

SLIDE 4 — ARCHITECTURE
Layout: centered diagram on light or white background (contrast with surrounding dark slides)

Title: How It Works

Architecture diagram (draw or illustrate):

TOP ROW — four boxes: "GitHub Copilot" | "Cursor" | "Codex" | "Claude / Custom"
Arrows pointing DOWN from all four into:
MIDDLE BOX — large box labeled "McpServer" with inner labels: "Context Search · TODO API · Session Log · GitHub Sync · GraphRAG · Requirements"
Arrow pointing DOWN into:
BOTTOM BOX — "Your Workspace" with inner labels: "Source Code · Docs · TODOs · GitHub Issues"

Caption below diagram: "McpServer is protocol-native. Agents connect over the open Model Context Protocol — no custom integration code required."

Speaker notes: McpServer implements MCP — the Model Context Protocol, an open standard for AI agent tool-calling. Any MCP-compatible agent connects out of the box. It also exposes a full REST API for direct integration from scripts, CI pipelines, or .NET applications.

---

SLIDE 5 — FEATURE: HYBRID CONTEXT SEARCH
Layout: left 55% text + bullets, right 45% code/output mockup

Title: Hybrid Context Search

Body text: Local FTS5 full-text + HNSW vector similarity (all-MiniLM-L6-v2 ONNX), fused with BM25 scoring.

Bullet points:
• Zero cloud dependency — all embeddings run on-device
• Keyword and semantic search combined
• Workspace-scoped — only your code, your docs
• Endpoint: GET /mcpserver/context/search

Right side: dark code block mockup showing a JSON response with fields like "score", "chunk", "source", "line" — representing a context search result.

Small tag in corner: "No API Key Required"

Speaker notes: The context search is the core feature. When an agent asks "what does this interface do?" or "find all places where this pattern is used," McpServer searches your actual codebase semantically. It runs 384-dimension vector embeddings locally using an ONNX model — no API key, no cloud, no data leaving your machine.

---

SLIDE 6 — FEATURE: TODO MANAGEMENT & SESSION LOGGING
Layout: two equal panels side by side, each with its own heading and icon

Title: Coordinated Work & Full Audit Trail

LEFT PANEL — blue-tinted card:
Icon: checklist
Heading: TODO Management
Text: CRUD API for structured TODO items. YAML or SQLite backend. Agents create, query, complete items. Bidirectional GitHub Issue sync.
Endpoint: GET/POST/PUT/DELETE /mcpserver/todo

RIGHT PANEL — purple-tinted card:
Icon: history / clock
Heading: Session Logging
Text: Every AI agent interaction captured. Query, response, decisions, actions, files modified, commit SHAs. Searchable. Attributed per agent.
Endpoint: POST /mcpserver/sessionlog

Speaker notes: These two features solve the coordination and auditability problems. Agents share a single TODO queue and can see what others are working on. The session log is a full audit trail — useful for code review, compliance, and resuming interrupted work.

---

SLIDE 7 — FEATURE: GITHUB ISSUE SYNC
Layout: centered, icon-led layout

Title: Your TODO List and GitHub Issues — In Sync

Icon at top: GitHub Octocat or sync arrows

Body text: McpServer syncs workspace TODO items bidirectionally with GitHub Issues.

Bullet points:
• Create GitHub Issues from TODO items automatically
• Close issues when TODOs are marked done
• Map ISSUE-* TODO IDs to GitHub issue numbers
• Filter by labels, state, and milestone

Visual: side-by-side mockup — left: a TODO item in McpServer marked "done"; right: the matching GitHub Issue shown as "closed". Double-headed arrow between them.

Link at bottom: https://github.com/sharpninja/McpServer

Speaker notes: For teams using GitHub Issues for project tracking, this means agents can interact with the issue tracker through the same TODO API they already use. Stakeholders see progress in GitHub. Agents don't need to know GitHub's API separately.

---

SLIDE 8 — FEATURE: GRAPHRAG
Layout: dark background (extra dark section for contrast), graph visualization center

Title: GraphRAG — Deep Codebase Reasoning

Body text: Optional graph-augmented retrieval that builds a knowledge graph over your codebase.

Four feature pills / badges:
• local mode — narrow, precise queries
• global mode — broad architectural summaries
• drift mode — change-aware queries
• Graceful fallback to hybrid search

Visual: a glowing knowledge graph — nodes representing code entities (classes, interfaces, modules) connected by labeled edges (calls, implements, depends on). Dark background with teal/blue node glow.

Small note: "Per-workspace · Optional · Zero cloud dependency"

Speaker notes: For complex architectural questions — "what components depend on this interface?" or "summarize the data flow through module X" — GraphRAG provides graph-structured reasoning over the codebase. It's optional, per-workspace, and degrades gracefully. Run it when you need it.

---

SLIDE 9 — UI TOOLING
Layout: 2×3 grid of cards on dark background

Title: The Right Interface for Every Workflow

Six cards:

Card 1 — icon: browser window
Name: Blazor Web UI
Text: Browser dashboard. Todos, sessions, agents, templates, context search.
Note: GitHub's Primer CSS design system

Card 2 — icon: terminal / >_
Name: Director CLI
Text: dotnet tool install --global SharpNinja.McpServer.Director
Link: https://www.nuget.org/packages/SharpNinja.McpServer.Director

Card 3 — icon: keyboard / TUI
Name: Director TUI
Text: director ui — full terminal UI, role-filtered tabs, auto-refresh

Card 4 — icon: puzzle piece / VS Code logo
Name: VS / VS Code Extension
Text: VSIX extension. Browse and update todos inside your editor.

Card 5 — icon: NuGet / package box
Name: Client NuGet
Text: SharpNinja.McpServer.Client — typed C# client for all API endpoints
Link: https://www.nuget.org/packages/SharpNinja.McpServer.Client

Card 6 — icon: plug / connect
Name: MCP STDIO / HTTP
Text: Connect Copilot, Cursor, Codex, or Claude with zero custom code
Endpoint: POST http://localhost:7147/mcp-transport

Speaker notes: We built six access surfaces because different people work differently. The CLI is for automation and scripting. The TUI is for SSH sessions. The Web UI is for exploration and team review. The VSIX is for developers who don't want to leave VS Code. The NuGet client is for building on top of McpServer. And the MCP transport is for agents.

---

SLIDE 10 — DEPLOYMENT
Layout: two columns, dark background

Title: Deploy Anywhere

LEFT COLUMN — icon list:
• Windows Service — managed install via Update-McpService.ps1
• Docker container — docker-compose.mcp.yml included
• MSIX package — enterprise / Store distribution
• Self-hosted — any .NET-capable host, any OS
• Multi-workspace — one process, unlimited projects

RIGHT COLUMN — three dark code blocks (monospace font, dark gray boxes):

Block 1 label: Windows Service
scripts\Update-McpService.ps1

Block 2 label: Docker
docker-compose -f docker-compose.mcp.yml up

Block 3 label: MSIX
scripts\Package-McpServerMsix.ps1

Speaker notes: McpServer is designed to fit into whatever deployment model you already use. Local developer machines, CI/CD containers, enterprise fleet management via MSIX — the same binary, the same config model, the same API surface.

---

SLIDE 11 — GETTING STARTED
Layout: step-by-step numbered callouts, dark background

Title: Running in Two Minutes

Step 1 — Build (dark code block):
dotnet restore McpServer.sln
dotnet build McpServer.sln -c Staging

Step 2 — Run (dark code block):
.\scripts\Start-McpServer.ps1 -Configuration Staging

Step 3 — Open Swagger:
http://localhost:7147/swagger

Step 4 — Connect your agent (dark code block):
{ "mcpServers": { "mcpserver": { "url": "http://localhost:7147/mcp-transport" } } }

CTA button below steps (blue filled): "View Full Documentation on GitHub →"
Link: https://github.com/sharpninja/McpServer

Small note at bottom: "AGENTS-README-FIRST.yaml is written to your workspace root on startup — agents auto-discover the API key and base URL."

Speaker notes: Four steps from zero to a running context server that any MCP agent can connect to. The marker file is written automatically on startup. Agents that read it get the API key, base URL, and connection prompt without any manual configuration.

---

SLIDE 12 — CALL TO ACTION
Layout: full-bleed dark background, large centered text

Main headline: Give Your Agents Memory.

Body text: McpServer is open source, self-hosted, and ready to run.

Three CTA blocks (stacked or side by side):

CTA 1 — filled blue button:
⭐ Star on GitHub
https://github.com/sharpninja/McpServer

CTA 2 — outlined button:
Install Director CLI
dotnet tool install --global SharpNinja.McpServer.Director
https://www.nuget.org/packages/SharpNinja.McpServer.Director

CTA 3 — outlined button:
Add the C# Client
dotnet add package SharpNinja.McpServer.Client
https://www.nuget.org/packages/SharpNinja.McpServer.Client

Footer links (small text at bottom):
GitHub: https://github.com/sharpninja/McpServer
Swagger: http://localhost:7147/swagger
License: MIT · © SharpNinja

Speaker notes: The repository is public. The NuGet packages are published. The documentation covers configuration, deployment, and API reference. If you're building with AI coding agents, give them a context server. Start here.

---

OVERALL DECK STYLE RULES:
- Slide backgrounds: deep navy #0d1117 (most slides), white/light gray #f6f8fa (Slide 4 architecture only)
- Primary accent: GitHub blue #0969da
- Secondary accent: teal #26a69a or cyan for icons and badges
- Headline font: bold, 40–52pt
- Body font: regular, 18–22pt
- Code blocks: dark gray background #161b22, white or light green monospace text, slightly rounded corners
- No clip art. No stock photo people. Technical diagrams, code snippets, and clean icons only.
- Slide numbers: bottom right, small, subtle
- Progress bar or thin blue line at bottom of each slide optional
```

---

## Usage Notes

- Paste the full prompt into **Canva AI → Magic Design → Presentation**
- If Canva limits prompt length, paste one `SLIDE N` block at a time and add slides sequentially
- After generation, upload the PNGs from `docs/Marketing/diagrams/` as custom images:
  - `architecture.png` → Slide 4
  - `features.png` → Slide 3 (feature map)
  - `ui-tooling.png` → Slide 9
  - `agent-workflow.png` → Slide 11
- All links in this prompt are real and ready to use as hyperlinks in Canva's link tool

## Known Links Reference

| Resource | URL |
|---|---|
| GitHub Repository | https://github.com/sharpninja/McpServer |
| Client NuGet | https://www.nuget.org/packages/SharpNinja.McpServer.Client |
| Director NuGet | https://www.nuget.org/packages/SharpNinja.McpServer.Director |
| Local Swagger UI | http://localhost:7147/swagger |
| MCP Transport | http://localhost:7147/mcp-transport |
| MCP Spec | https://modelcontextprotocol.io |
