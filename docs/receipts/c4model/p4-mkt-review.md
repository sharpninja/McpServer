# P4 MKT review record: Marketing diagrams as views of the C4 workspace

PLAN-C4MODEL-001 P4, DocCode MKT. Written 2026-10-19T16:21:18 local by the MKT conversion agent (ClaudeCode, claude-opus-5-5), session ClaudeCode-20261019T141815Z-plugin-session, turn req-20261019T202121Z-p4-mkt-convert.

- Worktree: /home/sharpninja/github/McpServer/.claude/worktrees/p4-mkt-20261019, branch p4/mkt-20261019, base 92e2c6c40ad61e80ef7ea486f189b0449acfe587. The four sources and both documents are unchanged between 1501f2b0 and 92e2c6c4 (git diff --stat 1501f2b0..HEAD -- docs/Marketing is empty; base-check.ps1 at 2026-10-19T15:41:48).
- Model: docs/architecture/c4/model/mkt.dsl. Views: docs/architecture/c4/views/mkt.dsl (23 views).
- Gate logs: docs/receipts/c4model/20261019T161902-p4-mkt-r9.log (run 1) and docs/receipts/c4model/20261019T162015-p4-mkt-gate-r2.log (run 2).

## Sources and where they were shown

The Marketing documents carry no Mermaid fences. The diagrams are four .mmd files shown through PNG image links, 8 links in all, each replaced in place by the image link of every view of that diagram followed by its "View:" line (D7 form):

- docs/Marketing/diagrams/agent-workflow.mmd (sequenceDiagram, line 14): docs/Marketing/presentation-outline.md line 133.
- docs/Marketing/diagrams/architecture.mmd (graph TB, line 14): presentation-outline.md line 43; docs/Marketing/website-copy.md lines 16 and 81.
- docs/Marketing/diagrams/features.mmd (mindmap, line 14): presentation-outline.md line 35; website-copy.md line 38.
- docs/Marketing/diagrams/ui-tooling.mmd (graph LR, line 14): presentation-outline.md line 102; website-copy.md line 65.

Document receipts (diff-style.ps1 at 2026-10-19T16:22:39, git diff against HEAD): presentation-outline.md removed 4 lines and added 92, website-copy.md removed 4 and added 85; the removed lines are exactly the 8 PNG image lines, the added lines are image, blank and "View:" lines, and no added line carries a dash or a table row. The pre-existing em dashes of both documents are surrounding marketing text and are left unchanged. Link pairs now: presentation-outline.md 23 (features 6, architecture 7, UI tooling 2, agent workflow 8), website-copy.md 22 (architecture 7, features 6, UI tooling 2, architecture 7). Every link resolves: DanglingViewLink 0 in both gate runs.

## 1. agent-workflow.mmd: sequence, 3 participants, 17 messages, 5 notes, 0 branches, 0 loops

Views (dynamic, scope qbrain): MKT-AGENTWORKFLOW-01, -part2, -part3, -part4, -part5, -part6, -part7, -part8. Each part has at most three participants, because four participants with request and reply edges between the same pair drew labels over the database (renders of runs 1 to 5 inspected).

Participants: Agent to agent (reused). QBrainAi to qbrain.server (reused). Workspace names no single element; each message goes to the element that handles it: qbrain.repoFs, qbrain.db, qbrain.vector or github (all reused).

Messages, source line, view and step:
1. Line 19 "Read AGENTS-README-FIRST.yaml": MKT-AGENTWORKFLOW-01 step 1, agent to qbrain.repoFs. Receiver changed from QBrainAi to Workspace Files (judgment J1).
2. Line 20 "API key, base URL, workspace path": MKT-AGENTWORKFLOW-01 step 2, reply qbrain.repoFs to agent.
3. Line 22 "POST /qbrainai/context/search": part2 step 1, agent to qbrain.server (REST relationship).
4. Line 23 "FTS5 + HNSW vector search": part2 step 2 to qbrain.db ("FTS5 full-text half") and part3 step 1 to qbrain.vector ("HNSW vector half").
5. Line 24 "Matching code chunks": part2 step 3 from qbrain.db and part3 step 2 from qbrain.vector.
6. Line 25 "Ranked context pack": part3 step 3, reply to agent.
7. Line 27 "GET /qbrainai/todo?section=backlog": part4 step 1.
8. Line 28 "TODO item list": part4 step 2, reply.
9. Line 29 "PUT /qbrainai/todo/{id}": part5 step 1.
10. Line 30 "Write TODO.yaml / SQLite": part5 step 2 to qbrain.db ("the SQLite row") and part6 step 1 to qbrain.repoFs ("the TODO.yaml projection").
11. Line 31 "Updated item": part6 step 2, reply.
12. Line 33 "POST /qbrainai/sessionlog": part7 step 1.
13. Line 34 "Persist session entry": part7 step 2, qbrain.server to qbrain.db.
14. Line 35 "Session ID confirmed": part7 step 3, reply.
15. Line 37 "POST /qbrainai/sync": part8 step 1 (text kept although the route does not exist, open question Q2).
16. Line 38 "Sync TODOs with GitHub Issues": part8 step 2, qbrain.server to github.
17. Line 39 "Sync result": part8 step 3, reply.

Notes: line 18 Session Start (part 1), line 21 Context Retrieval (parts 2 and 3), line 26 Task Management (parts 4 to 6), line 32 Session Logging (part 7), line 36 GitHub Sync (part 8); each note is the phase name in the view title.

Coverage: 17 of 17 messages; 20 view steps (messages 4, 5 and 10 go to two stores, one step each); 5 of 5 notes; 0 branches and 0 loops in the source, so no branch or loop views. Replies are drawn over the request relationship (the pinned CLI accepts a reverse step; probe C). Agent and server steps name the technology "HTTP REST, X-Api-Key", which selects the new REST relationship (probe C showed the technology selects the relationship).

## 2. architecture.mmd: graph, 20 nodes, 4 subgraphs, 31 edges

Views (component, scope qbrain.server): MKT-ARCHITECTURE-01 (agents and transports), -part2 (transports to tools and controllers), -part3 (multi-workspace routing), -part4 (context search and TODOs), -part5 (session logging and requirements), -part6 (GitHub issue sync), -part7 (GraphRAG and prompt templates).

Nodes:
- A1 GitHub Copilot, A2 Cursor, A3 Codex, A4 Claude / Custom: agent (4 nodes, 1 element; the model has one AI Coding Agent person, whose description names Codex, Claude Code and Copilot; Cursor is not named).
- T1 MCP STDIO: qbrain.server.stdioHost. T2 MCP Streamable HTTP /mcp-transport: qbrain.server.mcpHttp. T3 REST API /qbrainai/*: qbrain.server.api, entered through qbrain.server.middleware.
- C1 Hybrid Context Search: qbrain.server.search. C2 TODO Management: qbrain.server.todo. C3 Session Logging: qbrain.server.sessionLog. C4 GitHub Issue Sync: qbrain.server.github. C5 GraphRAG: qbrain.server.graphrag. C6 Requirements Tracking: qbrain.server.requirements. C7 Prompt Templates: qbrain.server.tools. C8 Multi-Workspace Routing: qbrain.server.middleware (WorkspaceResolutionMiddleware.cs:26, X-Workspace-Path) together with qbrain.server.workspace (the workspace resolution every transport reaches).
- W1 Source Code, W2 Documentation, W3 TODO.yaml, W5 Session Logs: qbrain.repoFs (4 nodes, 1 element). W4 GitHub Issues: github.
- Subgraphs: AI Agents, Transport Layer, QBrainAi (the qbrain.server boundary in every part) and Your Workspace (qbrain.repoFs plus github).

Edges:
- Line 44, A1..A4 to T1 (4 edges): agent to qbrain.server.stdioHost (existing); MKT-ARCHITECTURE-01.
- Line 44, A1..A4 to T2 (4 edges): agent to qbrain.server.mcpHttp (existing); MKT-ARCHITECTURE-01.
- Line 44, A1..A4 to T3 (4 edges): agent to qbrain.server.middleware (new), then middleware to api (existing); MKT-ARCHITECTURE-01 and -part2.
- Line 45, T1 to C8: stdioHost to mcpTools (part2) and mcpTools to workspace (part3), a path; STDIO never passes the HTTP middleware.
- Line 45, T2 to C8: mcpHttp to mcpTools (part2) and mcpTools to workspace (part3), a path.
- Line 45, T3 to C8: middleware to api (part2, reverse direction: the pipeline runs before the controllers) and api to workspace (part3).
- Line 46, C8 to C1..C7 (7 edges): routed requests reach the features through api and mcpTools, both of which have a relationship to each of the 7 feature components (14 existing relationships); parts 4 to 7.
- Line 47, C1, C2, C3 to Your Workspace (3 edges): search to repoFs, todo to repoFs (existing; parts 4 and 4) and sessionLog to repoFs (existing; part 5).
- Line 48, C4 to W4: qbrain.server.github to github (existing); part 6.
- Line 49, C5 to W1 and W2 (2 edges): graphrag to repoFs (new, GraphRagService.cs:740-789); part 7.
- Line 49, C6 to W1 and W2 (2 edges): requirements to repoFs (new); part 5. The code reads the docs/Project requirement documents (W2); reading source code (W1) is not evidenced.
- Line 50, C7 to W3: no model relationship; prompt templates do not write TODO.yaml (open question Q4). Not drawn.

Coverage: nodes 20 of 20 mapped to 15 elements; edges 30 of 31 represented (12 by three agent relationships, 3 by paths, 7 by the dispatcher relationships, 8 by six store relationships), 1 not drawn (C7 to W3).

## 3. features.mmd: mindmap, 25 nodes, 24 edges

Views (custom): MKT-FEATURES-01 (root and 5 areas: 6 nodes, 5 edges), -part2 Context and Search (5 nodes, 4 edges), -part3 Task Management (5, 4), -part4 Agent Coordination (5, 4), -part5 Requirements (4, 3), -part6 Infrastructure (5, 4).

Elements: 25 new custom elements tagged Topic (mktFeatures, mktFt*), one per mindmap node, each with a description naming the as-built element that realizes it and a realizedBy property; 24 relationships "includes" with technology "Feature map". The root is named "QBrainAi feature map" instead of "QBrainAi" (judgment J3). The bold markers (**) of the source are dropped. "YAML or SQLite Backend" keeps its name; its description states the as-built storage (open question Q3).

Coverage: nodes 25 of 25 (30 appearances, each area node in the overview and in its own part); edges 24 of 24 (5 + 4 + 4 + 4 + 3 + 4).

## 4. ui-tooling.mmd: graph, 7 nodes, 4 subgraphs, 6 edges

Views: MKT-UITOOLING-01 (container view of qbrain: web, terminal and command-line clients), MKT-UITOOLING-01-part2 (component view of qbrain.libs: client library and agents).

Nodes:
- MCP "QBrainAi http://localhost:7147": qbrain.server (reused).
- WEB Blazor Web UI: mktWebUi, new external software system tagged External and Planned (commit 6e1a818b removed the migrated src/McpServer.Web; TR-MCP-WEB-001 gives the web UI to McpServerManager; FR-MCP-031 is Planned).
- TUI Director TUI and CLI Director CLI: mktDirector, new external software system (2 nodes, 1 element: director ui is a command of the director tool, docs/Marketing/ui-tooling.md lines 9 and 12; moved to McpServerManager per FR-MCP-039 and Requirements-Director.md lines 5 to 8).
- VSIX VS / VS Code Extension: not modeled; commit e1e006dc (2026-03-06) removed the extension and scripts/Package-Vsix.ps1 (open question Q1).
- NUGET Client NuGet QBrainAI.Client: qbrain.libs.client (reused).
- STDIO MCP STDIO / HTTP (Copilot, Cursor, Codex, Claude): agent (reused).

Edges:
- Line 30 WEB to MCP "HTTP REST": mktWebUi to qbrain.server (new, Planned); MKT-UITOOLING-01.
- Line 31 TUI to CLI "via Director": inside mktDirector; no relationship.
- Line 32 CLI to MCP "HTTP REST + API Key": mktDirector to qbrain.server (new); MKT-UITOOLING-01.
- Line 33 VSIX to MCP: not modeled.
- Line 34 NUGET to MCP "HTTP REST + DI": qbrain.libs.client to qbrain.server "REST" (existing) and the implied "REST requests" from client to middleware; part 2.
- Line 35 STDIO to MCP "MCP STDIO or /mcp-transport": agent to qbrain.server "Calls MCP tools" (existing); part 2, which also shows the new agent REST relationship.

Coverage: nodes 6 of 7 mapped (5 elements), 1 not modeled; edges 4 of 6 drawn, 1 internal to one element, 1 not modeled.

## Renders inspected as images

All 23 MKT renders were converted with svg-to-png.ps1 at letter size into SCRATCH/p4-mkt/shots and read as images after the final layout: 8 dynamic (sequence) views, 9 component or container views (architecture and UI tooling), 6 custom (mindmap) views; every one is a split part. Checked on each: every source element and message of the part is present, steps are numbered in source order, no element overlaps another, labels are legible and not hidden under elements, Planned web UI is drawn dashed, and the page check passes (PageOverflow 0, TextSizeSpread 0 in both gate runs; the letter-page fit of the 23 renders lies between scale 0.648 and 0.947 against the 0.635 that 8 pt text needs, findings-mkt.ps1). Layout iterations (logs 20261019T155852-p4-mkt-r1 to 20261019T161700-p4-mkt-r8): run 1 had 3 page overflows (architecture part 1 and the first part 5, UI tooling), runs 2, 3 and 6 had 2 each (sequence parts with four participants or three in a row), runs 4, 5, 7 and 8 had 0; label overlap was removed by splitting sequence parts to at most three participants and choosing the autolayout per view. Remaining cosmetic points: in architecture parts 4, 5 and 7 the labels of the crossing api and mcpTools edges sit close together but stay readable.

## Gate G4

Run 1 (20261019T161902-p4-mkt-r9.log) and run 2 (20261019T162015-p4-mkt-gate-r2.log): ValidationFailed 0, InspectionErrors 0, RenderFailed 0, DanglingViewLink 0, MissingRender 0, OrphanRender 0, TextSizeSpread 0, PageOverflow 0, InvalidSvg 0, MermaidFence 435 (all in other documents; 0 lines match "MermaidFence docs/Marketing"), MermaidFile 4 (exactly the four Marketing .mmd files, expected until the operator clears their deletion). Byte-identical check: hash-dir.ps1 manifest after run 1, compare after run 2: "Identical: 236 of 236" (2026-10-19T16:21:05).

## Judgment calls

- J1: the marker read is drawn to Workspace Files, not to the server, because no server endpoint returns the marker (Program.cs:936 serves only /marker-file-timestamp) and FR-MCP-038 and the slide notes (presentation-outline.md line 135) say agents read the file.
- J2: messages that address two stores are one step per store; Workspace is mapped per message.
- J3: the mindmap root is "QBrainAi feature map", because custom views cannot include the qbrain software system and D4 forbids a second element for the product.
- J4: keys use the diagram file name without its hyphen (MKT-AGENTWORKFLOW-01, MKT-UITOOLING-01) so the name stays one key field before -partN.

## Open questions for the operator

- Q1: the VS and VS Code extension was removed (e1e006dc) but the slide card 4, website tool 4, docs/Marketing/ui-tooling.md and docs/MCP-SERVER.md lines 342 to 349 still offer it.
- Q2: POST /qbrainai/sync has no route; GitHub sync is POST /qbrainai/gh/issues/sync/from-github and /to-github (GitHubController.cs:417, 435).
- Q3: "YAML or SQLite Backend" and the slide text "YAML or SQLite backend" are stale: the yaml provider was removed (TR-MCP-CFG-007, docs/MCP-SERVER.md line 79).
- Q4: architecture.mmd draws Prompt Templates writing TODO.yaml; no code does.
- Q5: TR-WEB-UI-001 (pending) asks for src/QBrainAi.Web in this repository, while TR-MCP-WEB-001 gives web UI work to McpServerManager.
