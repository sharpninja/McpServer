# Marketing diagrams (PLAN-C4MODEL-001 P4, DocCode MKT): what the four docs/Marketing diagrams need beyond the
# as-built model. Sources: docs/Marketing/diagrams/{agent-workflow,architecture,features,ui-tooling}.mmd at 92e2c6c4.
# Every relationship and element below names its evidence; nothing here is invented for the marketing story.

# As-built agent paths drawn by the agent workflow and architecture diagrams.
agent -> qbrain.server "Calls REST endpoints under /qbrainai/* with the marker's API key" "HTTP REST, X-Api-Key" {
    properties {
        "evidence" "Controllers/*.cs routes qbrainai/* (ContextController.cs:25, TodoController.cs:14, SessionLogController.cs:15); WorkspaceAuthMiddleware protects /qbrainai/* (evidence.md Step A); FR-MCP-038 and the marker give agents the base URL and API key."
        "source" "docs/Marketing/diagrams/agent-workflow.mmd lines 22, 27, 29, 33, 37; architecture.mmd line 24 (REST API /qbrainai/*)"
    }
}
agent -> qbrain.server.middleware "REST requests to /qbrainai/* (X-Api-Key, X-Workspace-Path)" "HTTP REST" "H" {
    properties {
        "evidence" "src/QBrainAi.Support.Mcp/Middleware/WorkspaceResolutionMiddleware.cs:26 (X-Workspace-Path) and WorkspaceAuthMiddleware (evidence.md Step A); the same entry as qbrain.libs.client -> qbrain.server.middleware."
        "source" "docs/Marketing/diagrams/architecture.mmd lines 24 and 44 (agents to REST API)"
    }
}
agent -> qbrain.repoFs "Reads AGENTS-README-FIRST.yaml (API key, base URL, workspace path)" "File read" {
    properties {
        "evidence" "FR-MCP-038 (at session start, read the marker file); MarkerFileService writes the marker into the workspace (evidence.md, Workspace Service); no server endpoint returns the marker (Program.cs:936 serves only /marker-file-timestamp)."
        "source" "docs/Marketing/diagrams/agent-workflow.mmd lines 19 to 20; docs/Marketing/presentation-outline.md line 135"
    }
}

# As-built file access of two server components (architecture diagram, features to workspace).
qbrain.server.graphrag -> qbrain.repoFs "Reads its input corpus and writes the graph under mcp-data/graphrag in the workspace" "File I/O" "H" {
    properties {
        "evidence" "src/QBrainAi.GraphRag/Options/GraphRagOptions.cs:22 (RootPath mcp-data/graphrag); Services/GraphRagService.cs:740-747 (graph root under the workspace path), 769-789 (reads input files), 940 (writes the ready artifact)."
        "source" "docs/Marketing/diagrams/architecture.mmd line 49 (GraphRAG to Source Code and Documentation)"
    }
}
qbrain.server.requirements -> qbrain.repoFs "Imports FR, TR and TEST documents from docs/Project when the store is empty" "File I/O" "H" {
    properties {
        "evidence" "src/QBrainAi.Services/Requirements/RequirementsDatabaseDocumentService.cs:1358-1371 (bootstrap import of the Functional, Technical, Testing and Mapping documents), 1571-1572 (ReadFileIfExists)."
        "source" "docs/Marketing/diagrams/architecture.mmd line 49 (Requirements Tracking to Source Code and Documentation)"
    }
}

# UI clients outside this repository (UI tooling diagram). Director, Web and UI.Core were removed from this repository
# as migrated projects by commit 6e1a818b (2026-03-06, ARCH-MIGRATE-002); McpServerManager owns them
# (FR-MCP-039, docs/Project/Requirements-Director.md lines 5-8, TR-MCP-WEB-001).
mktDirector = softwareSystem "Director CLI and TUI" "director, the .NET global tool SharpNinja.QBrainAi.Director: CLI commands and the director ui terminal UI. Lives in the McpServerManager repository." "External" {
    properties {
        "evidence" "docs/Project/Requirements-Director.md lines 5-8 and 20 (moved to McpServerManager; commands call the server through McpHttpClient with marker connection details); Requirements-Matrix.md FR-MCP-030 Complete; commit 6e1a818b removed src/McpServer.Director."
        "source" "docs/Marketing/diagrams/ui-tooling.mmd lines 18 and 21 (Director TUI, Director CLI)"
    }
}
mktWebUi = softwareSystem "Management Web UI" "Blazor web dashboard for workspaces, TODOs, sessions, templates, agents and context. Owned by the McpServerManager repository; FR-MCP-031 is Planned." "External,Planned" {
    properties {
        "evidence" "Commit 6e1a818b removed the migrated src/McpServer.Web (2026-03-06); TR-MCP-WEB-001 (Web UI implementation owned by McpServerManager); Requirements-Matrix.md line 56 FR-MCP-031 Planned; TR-WEB-UI-001 (pending) asks for src/QBrainAi.Web in this repository instead (open question)."
        "source" "docs/Marketing/diagrams/ui-tooling.mmd line 17 (Blazor Web UI)"
    }
}
mktDirector -> qbrain.server "HTTP REST + API Key" "HTTP/JSON, X-Api-Key" {
    properties {
        "evidence" "docs/Project/Requirements-Director.md line 20 (all commands call the server via McpHttpClient, connection details from AGENTS-README-FIRST.yaml)."
        "source" "docs/Marketing/diagrams/ui-tooling.mmd line 32"
    }
}
mktWebUi -> qbrain.server "HTTP REST" "HTTP/JSON" "Planned" {
    properties {
        "evidence" "TR-MCP-WEB-002 and TR-MCP-WEB-003 (external web-management clients use the server REST API with workspace auth); deferred."
        "source" "docs/Marketing/diagrams/ui-tooling.mmd line 30"
    }
}

# Feature map (features.mmd mindmap): marketing topics, one Topic element per mindmap node. Each description names
# the as-built element that realizes the topic; the realizedBy property carries its identifier.
mktFeatures = element "QBrainAi feature map" "" "Root of the marketing feature map of the QBrain.AI software system." "Topic" {
    properties {
        "realizedBy" "qbrain"
        "source" "docs/Marketing/diagrams/features.mmd line 15"
    }
}
mktFtContext = element "Context and Search" "" "Feature area: Context Search and Ingestion, GraphRAG." "Topic" {
    properties {
        "realizedBy" "qbrain.server.search, qbrain.server.graphrag"
        "source" "docs/Marketing/diagrams/features.mmd line 16"
    }
}
mktFtTasks = element "Task Management" "" "Feature area: TODO Service and GitHub Integration." "Topic" {
    properties {
        "realizedBy" "qbrain.server.todo, qbrain.server.github"
        "source" "docs/Marketing/diagrams/features.mmd line 21"
    }
}
mktFtCoordination = element "Agent Coordination" "" "Feature area: Session Log Service." "Topic" {
    properties {
        "realizedBy" "qbrain.server.sessionLog"
        "source" "docs/Marketing/diagrams/features.mmd line 26"
    }
}
mktFtRequirements = element "Requirements" "" "Feature area: Requirements and Use Cases." "Topic" {
    properties {
        "realizedBy" "qbrain.server.requirements"
        "source" "docs/Marketing/diagrams/features.mmd line 31"
    }
}
mktFtInfrastructure = element "Infrastructure" "" "Feature area: server host, transports, templates, deployment." "Topic" {
    properties {
        "realizedBy" "qbrain.server"
        "source" "docs/Marketing/diagrams/features.mmd line 35"
    }
}
mktFtHybridSearch = element "Hybrid FTS5 + Vector Search" "" "Context Search and Ingestion (HybridSearchService)." "Topic" {
    properties {
        "realizedBy" "qbrain.server.search"
        "evidence" "src/QBrainAi.Services/Services/HybridSearchService.cs:9"
        "source" "docs/Marketing/diagrams/features.mmd line 17"
    }
}
mktFtEmbeddings = element "384-dim ONNX Embeddings" "" "EmbeddingService, all-MiniLM-L6-v2 from Hugging Face." "Topic" {
    properties {
        "realizedBy" "qbrain.server.search, hf"
        "evidence" "src/QBrainAi.Storage/Indexing/EmbeddingService.cs:13"
        "source" "docs/Marketing/diagrams/features.mmd line 18"
    }
}
mktFtBm25 = element "BM25 Score Fusion" "" "FTS5 bm25 ranks fused with vector hits (RRF)." "Topic" {
    properties {
        "realizedBy" "qbrain.server.search"
        "evidence" "src/QBrainAi.Services/Services/Fts5SearchService.cs:56 (bm25); HybridSearchService.cs:9 (Reciprocal Rank Fusion)"
        "source" "docs/Marketing/diagrams/features.mmd line 19"
    }
}
mktFtGraphRag = element "GraphRAG Knowledge Graph" "" "GraphRAG component, per workspace." "Topic" {
    properties {
        "realizedBy" "qbrain.server.graphrag"
        "source" "docs/Marketing/diagrams/features.mmd line 20"
    }
}
mktFtTodoApi = element "TODO CRUD API" "" "REST Controllers and TODO Service (/qbrainai/todo)." "Topic" {
    properties {
        "realizedBy" "qbrain.server.api, qbrain.server.todo"
        "evidence" "src/QBrainAi.Support.Mcp/Controllers/TodoController.cs:14, 52, 143, 162"
        "source" "docs/Marketing/diagrams/features.mmd line 22"
    }
}
mktFtTodoBackend = element "YAML or SQLite Backend" "" "MCP Database only; TODO.yaml is a read-only projection." "Topic" {
    properties {
        "realizedBy" "qbrain.server.todo, qbrain.db"
        "note" "Stale marketing claim: the yaml TODO storage provider was removed (TR-MCP-CFG-007, docs/MCP-SERVER.md line 79, McpInstanceResolver.cs:243)."
        "source" "docs/Marketing/diagrams/features.mmd line 23"
    }
}
mktFtIssueSync = element "GitHub Issue Sync" "" "GitHub Integration (IssueTodoSyncService)." "Topic" {
    properties {
        "realizedBy" "qbrain.server.github"
        "evidence" "src/QBrainAi.Support.Mcp/Controllers/GitHubController.cs:417, 435 (issues/sync/from-github, to-github)"
        "source" "docs/Marketing/diagrams/features.mmd line 24"
    }
}
mktFtTodoFilters = element "Priority and Section Filters" "" "GET /qbrainai/todo priority and section queries." "Topic" {
    properties {
        "realizedBy" "qbrain.server.api, qbrain.server.todo"
        "evidence" "src/QBrainAi.Support.Mcp/Controllers/TodoController.cs:52-58"
        "source" "docs/Marketing/diagrams/features.mmd line 25"
    }
}
mktFtSessionLogging = element "Session Logging" "" "Session Log Service (/qbrainai/sessionlog)." "Topic" {
    properties {
        "realizedBy" "qbrain.server.sessionLog"
        "evidence" "src/QBrainAi.Support.Mcp/Controllers/SessionLogController.cs:15, 38"
        "source" "docs/Marketing/diagrams/features.mmd line 27"
    }
}
mktFtAuditTrail = element "Audit Trail" "" "Session log turns and actions; TODO audit history." "Topic" {
    properties {
        "realizedBy" "qbrain.server.sessionLog, qbrain.server.todo"
        "evidence" "src/QBrainAi.Support.Mcp/Controllers/TodoController.cs:84 (GET {id}/audit)"
        "source" "docs/Marketing/diagrams/features.mmd line 28"
    }
}
mktFtDecisions = element "Decision Capture" "" "Design decisions recorded on session-log turns." "Topic" {
    properties {
        "realizedBy" "qbrain.server.sessionLog"
        "source" "docs/Marketing/diagrams/features.mmd line 29"
    }
}
mktFtSharedState = element "Multi-Agent Shared State" "" "One TODO store, context index and session log per workspace." "Topic" {
    properties {
        "realizedBy" "qbrain.server, qbrain.db"
        "source" "docs/Marketing/diagrams/features.mmd line 30"
    }
}
mktFtReqTracking = element "FR / TR / TEST Tracking" "" "Requirements and Use Cases." "Topic" {
    properties {
        "realizedBy" "qbrain.server.requirements"
        "source" "docs/Marketing/diagrams/features.mmd line 32"
    }
}
mktFtTraceability = element "Traceability Matrix" "" "Requirements-Matrix.md rendered from the store." "Topic" {
    properties {
        "realizedBy" "qbrain.server.requirements"
        "evidence" "src/QBrainAi.Services/Requirements/RequirementsDatabaseDocumentService.cs:1531 (RequirementsDocumentRenderer.MatrixFileName)"
        "source" "docs/Marketing/diagrams/features.mmd line 33"
    }
}
mktFtReqGeneration = element "Requirements Generation" "" "Requirements documents rendered from the store." "Topic" {
    properties {
        "realizedBy" "qbrain.server.requirements"
        "source" "docs/Marketing/diagrams/features.mmd line 34"
    }
}
mktFtMultiWorkspace = element "Multi-Workspace Hosting" "" "Workspace routing by the X-Workspace-Path header." "Topic" {
    properties {
        "realizedBy" "qbrain.server.middleware, qbrain.server.workspace"
        "evidence" "src/QBrainAi.Support.Mcp/Middleware/WorkspaceResolutionMiddleware.cs:26"
        "source" "docs/Marketing/diagrams/features.mmd line 36"
    }
}
mktFtTransports = element "HTTP REST + MCP STDIO" "" "REST Controllers, MCP HTTP Transport, MCP STDIO Host." "Topic" {
    properties {
        "realizedBy" "qbrain.server.api, qbrain.server.mcpHttp, qbrain.server.stdioHost"
        "source" "docs/Marketing/diagrams/features.mmd line 37"
    }
}
mktFtTemplates = element "Prompt Template Engine" "" "Tool Registry, Templates and Products." "Topic" {
    properties {
        "realizedBy" "qbrain.server.tools"
        "source" "docs/Marketing/diagrams/features.mmd line 38"
    }
}
mktFtContainers = element "Containerized Deployment" "" "Dockerfile and docker-compose.mcp.yml." "Topic" {
    properties {
        "realizedBy" "DeploymentDocker view"
        "source" "docs/Marketing/diagrams/features.mmd line 39"
    }
}

mktFeatures -> mktFtContext "includes" "Feature map"
mktFeatures -> mktFtTasks "includes" "Feature map"
mktFeatures -> mktFtCoordination "includes" "Feature map"
mktFeatures -> mktFtRequirements "includes" "Feature map"
mktFeatures -> mktFtInfrastructure "includes" "Feature map"
mktFtContext -> mktFtHybridSearch "includes" "Feature map"
mktFtContext -> mktFtEmbeddings "includes" "Feature map"
mktFtContext -> mktFtBm25 "includes" "Feature map"
mktFtContext -> mktFtGraphRag "includes" "Feature map"
mktFtTasks -> mktFtTodoApi "includes" "Feature map"
mktFtTasks -> mktFtTodoBackend "includes" "Feature map"
mktFtTasks -> mktFtIssueSync "includes" "Feature map"
mktFtTasks -> mktFtTodoFilters "includes" "Feature map"
mktFtCoordination -> mktFtSessionLogging "includes" "Feature map"
mktFtCoordination -> mktFtAuditTrail "includes" "Feature map"
mktFtCoordination -> mktFtDecisions "includes" "Feature map"
mktFtCoordination -> mktFtSharedState "includes" "Feature map"
mktFtRequirements -> mktFtReqTracking "includes" "Feature map"
mktFtRequirements -> mktFtTraceability "includes" "Feature map"
mktFtRequirements -> mktFtReqGeneration "includes" "Feature map"
mktFtInfrastructure -> mktFtMultiWorkspace "includes" "Feature map"
mktFtInfrastructure -> mktFtTransports "includes" "Feature map"
mktFtInfrastructure -> mktFtTemplates "includes" "Feature map"
mktFtInfrastructure -> mktFtContainers "includes" "Feature map"
