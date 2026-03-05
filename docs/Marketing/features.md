# McpServer — Feature Reference

## Feature 1 — Hybrid Context Search
**What it does:** Combines SQLite FTS5 full-text search with HNSW vector similarity (384-dim all-MiniLM-L6-v2 ONNX) fused via BM25 scoring. Runs entirely locally — no cloud API required.
**Why it matters:** Agents get semantically relevant, workspace-scoped context fast. Keyword search catches exact terms; vector search catches concepts.
**Endpoints:** `GET /mcpserver/context/search`, `POST /mcpserver/context/pack`

---

## Feature 2 — TODO Management
**What it does:** Full CRUD and query API for TODO items. YAML file-backed or SQLite table-backed storage. Accessible over HTTP REST and MCP STDIO.
**Why it matters:** TODO items become a structured API resource. Agents create, update, complete, and filter TODOs programmatically.
**Endpoints:** `GET/POST/PUT/DELETE /mcpserver/todo`

---

## Feature 3 — Session Logging
**What it does:** Ingests structured session log entries — queries, responses, decisions, actions, files modified, commits — in a searchable SQLite store.
**Why it matters:** Every agent interaction is audited. Design decisions are preserved. Work can be resumed across sessions.
**Endpoints:** `GET/POST /mcpserver/sessionlog`

---

## Feature 4 — GitHub Issue Sync
**What it does:** Bidirectional sync between workspace TODO items and GitHub Issues. Creates, updates, and closes issues to match TODO state.
**Why it matters:** Agents interact with GitHub Issues through the same TODO API. No manual copy-paste between TODO systems and issue trackers.
**Endpoints:** `GET/POST /mcpserver/gh`, `POST /mcpserver/sync`

---

## Feature 5 — Multi-Workspace Hosting
**What it does:** A single McpServer process hosts multiple workspaces, each with isolated context index, TODO storage, session log, and API key.
**Why it matters:** One server process serves an entire team's projects. Workspaces are registered dynamically via API or Director CLI.
**Endpoints:** `GET/POST /mcpserver/workspace`

---

## Feature 6 — GraphRAG
**What it does:** Optional workspace-scoped Graph RAG — builds a knowledge graph over your codebase. Supports `local`, `global`, and `drift` query modes. Falls back gracefully to hybrid search.
**Why it matters:** Deep codebase reasoning — "what components depend on this interface?" — not just keyword proximity.
**Endpoints:** `GET /mcpserver/graphrag/status`, `POST /mcpserver/graphrag/index`, `POST /mcpserver/graphrag/query`

---

## Feature 7 — Dual Transport (HTTP + MCP STDIO)
**What it does:** HTTP REST with Swagger/OpenAPI + MCP Streamable HTTP and STDIO simultaneously.
**Why it matters:** Works with every MCP agent (Copilot, Cursor, Codex, Claude) and any HTTP client or CI pipeline.
**Transports:** `POST /mcp-transport`, `--transport stdio` flag

---

## Feature 8 — Requirements Traceability
**What it does:** FR, TR, TEST, and FR→TR mapping as first-class API resources. Generates traceability matrix documents on demand.
**Why it matters:** Agents query which requirements they are implementing and generate compliance evidence.
**Endpoints:** `GET/POST/PUT/DELETE /mcpserver/requirements`

---

## Feature 9 — Prompt Template Engine
**What it does:** Stores, retrieves, and renders Handlebars-based prompt templates via API.
**Why it matters:** Prompt engineering becomes a versioned, API-managed asset.
**Endpoints:** `GET/POST/PUT/DELETE /mcpserver/templates`

---

## Feature 10 — Containerized Distribution
**What it does:** Ships as Dockerfile, Windows Service, and MSIX package. CI pipeline produces all artifacts automatically.
**Why it matters:** Deploys anywhere — local machine, Docker, Windows Server, enterprise fleet.
**Packaging:** `Dockerfile`, `docker-compose.mcp.yml`, `scripts/Package-McpServerMsix.ps1`
