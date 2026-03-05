# Canva AI — Website Generation Prompt

Paste the prompt below directly into Canva's AI website generator (Magic Design → Website, or the AI website builder prompt field). The prompt is self-contained — no prior context needed.

---

## Prompt

```
Create a professional developer-tool marketing website for a product called McpServer with the following structure, content, and style.

---

STYLE & DESIGN
- Dark hero section (deep navy #0d1117 or near-black) with high contrast white headlines
- Light sections below the hero (white or very light gray #f6f8fa background)
- Accent color: GitHub blue #0969da for buttons, links, icons, and highlights
- Secondary accent: bright teal or cyan for feature icons
- Typography: clean modern sans-serif (Inter or similar); large bold headlines; readable body text at 16–18px
- Design language: GitHub-inspired — clean, technical, minimal, professional
- No gradients or flashy effects; subtle shadows and borders only
- Mobile responsive layout

---

SECTION 1 — HERO (dark background)

Headline: "Your AI Agents, Finally Coordinated."

Subheadline: "McpServer is a self-hosted context server that gives AI coding agents persistent memory, shared workspace state, and structured task management — using the open Model Context Protocol standard."

Two buttons:
- Primary (filled blue): "Get Started on GitHub →"
- Secondary (outlined): "View Documentation"

Visual: A clean architecture diagram showing three AI agent boxes (GitHub Copilot, Cursor, Codex) with arrows pointing down into a central "McpServer" box, which has an arrow pointing down into a "Your Workspace" box (Source Code, Docs, TODOs, GitHub Issues). Use a dark-themed diagram with blue connectors.

---

SECTION 2 — PROBLEM (light background)

Small label above headline: "THE PROBLEM"

Headline: "AI Agents Are Powerful. But Stateless."

Body text: "Every session starts fresh. Your agents forget last session's decisions, don't know what other agents are working on, and fill in gaps with training data instead of your actual code. The result: repeated work, contradictory decisions, and no audit trail."

Three horizontal cards with icons:
Card 1 — icon: broken chain or hourglass
  Title: "Lost Context"
  Text: "Design decisions made in one session disappear in the next. Agents re-research. Consistency breaks."

Card 2 — icon: disconnected nodes or silos
  Title: "No Coordination"
  Text: "Multiple agents. Multiple tools. No shared state. Each agent is an isolated silo."

Card 3 — icon: question mark or ghost
  Title: "Hallucinated Context"
  Text: "Without access to your workspace, agents guess from training data — not from your code."

---

SECTION 3 — SOLUTION (white background)

Small label: "THE SOLUTION"

Headline: "One Server. All Agents. One Truth."

Body text: "McpServer runs locally alongside your codebase. It gives every connected agent the same view: your todos, your session history, your code — semantically searchable, always current. Agents coordinate through McpServer instead of working in isolation. Every decision is logged. Every action is audited. Context is retrieved, not hallucinated."

Visual: Dashboard screenshot placeholder (use a mockup of a clean dark-themed web admin panel with a sidebar and data tables).

Three supporting columns below the body:
Column 1 — "Persistent Memory" — "Session logs capture every decision, action, and design choice. Agents resume with full context."
Column 2 — "Shared Workspace State" — "All agents read from and write to the same TODO store, context index, and session log."
Column 3 — "Grounded Context" — "Semantic search over your actual code and docs — not training data guesses."

---

SECTION 4 — FEATURES (very light gray background)

Small label: "KEY FEATURES"

Headline: "Everything your agents need. Nothing they don't."

Six feature cards in a 3×2 grid. Each card has a colored icon (use blue/teal/purple tones), a title, and a short description.

Card 1 — icon: magnifying glass with sparkle
  Title: "Hybrid Context Search"
  Text: "FTS5 full-text + HNSW vector search (all-MiniLM-L6-v2), fused with BM25 scoring. Runs entirely locally. No cloud API."

Card 2 — icon: checklist
  Title: "TODO Management"
  Text: "Full CRUD todo API with YAML or SQLite backend. Agents query, create, and complete tasks programmatically."

Card 3 — icon: history clock
  Title: "Session Logging"
  Text: "Every agent interaction captured — queries, decisions, commits, files modified. Full audit trail, searchable."

Card 4 — icon: GitHub Octocat or sync arrows
  Title: "GitHub Issue Sync"
  Text: "Bidirectional sync between workspace TODOs and GitHub Issues. Agents interact with issues through the same API."

Card 5 — icon: network graph nodes
  Title: "GraphRAG"
  Text: "Graph-augmented retrieval for deep codebase reasoning. Local, optional, per-workspace. Falls back gracefully."

Card 6 — icon: folders or layers
  Title: "Multi-Workspace"
  Text: "One server. Many projects. Isolated context, todos, and keys per workspace. Routing via HTTP header."

---

SECTION 5 — UI TOOLING (white background)

Small label: "UI TOOLING"

Headline: "The right interface for every workflow."

Six tool cards in a 2-column layout, each with a tool icon, name, and one-line description:

Tool 1 — browser/dashboard icon
  Name: "Blazor Web UI"
  Text: "Full browser dashboard. Todos, sessions, agents, templates, context search."

Tool 2 — terminal/command icon
  Name: "Director CLI"
  Text: "dotnet tool install --global SharpNinja.McpServer.Director"

Tool 3 — terminal/TUI icon
  Name: "Director TUI"
  Text: "director ui — full terminal UI, tabs, keyboard nav, auto-refresh."

Tool 4 — puzzle piece / VS Code icon
  Name: "VS / VS Code Extension"
  Text: "VSIX extension. Browse and update todos inside your editor."

Tool 5 — NuGet / package icon
  Name: "Client NuGet"
  Text: "SharpNinja.McpServer.Client — typed C# client for all endpoints."

Tool 6 — plug / MCP icon
  Name: "MCP STDIO / HTTP"
  Text: "Native MCP transport. Connect Copilot, Cursor, Codex, or Claude with zero custom code."

---

SECTION 6 — ARCHITECTURE (light blue-gray tint background)

Small label: "HOW IT WORKS"

Headline: "Simple by design. Powerful at scale."

Center a clean architecture diagram with three layers:
Top layer: four boxes labeled "GitHub Copilot", "Cursor", "Codex", "Claude / Custom Agent" — connected by downward arrows to middle layer
Middle layer: single large box labeled "McpServer" with sub-labels: "Context Search | TODO API | Session Log | GitHub Sync | GraphRAG | Requirements"
Bottom layer: single box labeled "Your Workspace" with sub-labels: "Source Code · Docs · TODOs · GitHub Issues"

Caption below: "McpServer sits between your AI agents and your workspace. Agents read from it and write to it. Your workspace stays as the source of truth."

---

SECTION 7 — QUICK START (dark background, same as hero)

Small label: "GET STARTED"

Headline: "Running in under two minutes."

Three numbered steps displayed side by side:

Step 1 — "Build"
Code block (dark background, monospace font):
dotnet restore McpServer.sln
dotnet build McpServer.sln -c Staging

Step 2 — "Run"
Code block:
.\scripts\Start-McpServer.ps1 -Configuration Staging

Step 3 — "Connect"
Code block:
http://localhost:7147/swagger

Button below: "View Full Documentation →" (filled blue)

---

SECTION 8 — FOOTER (very dark background)

Tagline: "McpServer — Context intelligence for AI-assisted development."

Four footer link groups:
Group 1 — "Project": GitHub Repository, Documentation, Changelog
Group 2 — "Packages": NuGet: Client, NuGet: Director
Group 3 — "Tooling": Swagger UI, VS Extension, Docker
Group 4 — "Community": Issues, Discussions, Contribute

Bottom bar: "© SharpNinja · MIT License"

---

OVERALL TONE
Professional, technical, and confident. This is developer infrastructure — not a consumer app. No emojis. No playful copy. Precision and clarity over marketing fluff. Think: GitHub's own product pages, Vercel, or Cloudflare's developer documentation style.
```

---

## Tips for Using in Canva

- Paste the full prompt into **Canva AI → Magic Design → Website**
- If Canva breaks it up by section, paste one section at a time
- Upload the PNGs from `docs/Marketing/diagrams/` as image assets to use in the Architecture and Hero sections
- The code blocks in Quick Start work best as dark-background text boxes with a monospace font (Canva's "Code" text style if available, or a dark rectangle with white Courier/Source Code Pro text)
- For feature icons, use Canva's built-in icon library searching the terms in parentheses next to each icon description
