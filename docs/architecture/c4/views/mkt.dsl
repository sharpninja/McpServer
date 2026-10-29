# Views of the four Marketing diagrams (PLAN-C4MODEL-001 P4, DocCode MKT). Keys are MKT-<diagram>-01; a diagram that
# does not fit one letter page (TR-MCP-NUKE-017 AC-5) continues in parts keyed MKT-<diagram>-01-partN, in order.
# Sources: docs/Marketing/diagrams/*.mmd at 92e2c6c4; review record docs/receipts/c4model/p4-mkt-review.md.
# D3: the static views show as-built relationships, so they exclude relationships tagged Planned, which other documents
# add between shared elements (for example the SessionTurn self-steps). MKT-UITOOLING-01 shows the planned Web UI and
# its Planned edge on purpose, so it excludes only the server self-step.

# agent-workflow.mmd (sequence, 17 messages): dynamic views in source order, one or two per phase note, at most three
# participants each so the steps stay readable. A message sent to two stores at once (the FTS5 and HNSW halves of the
# search, the SQLite row and the TODO.yaml projection) is one step to each store; the halves sit in consecutive parts.
dynamic qbrain "MKT-AGENTWORKFLOW-01" "Agent workflow part 1 of 8, Session Start (agent-workflow.mmd lines 18 to 20)." {
    title "Agent Workflow Sequence (1 of 8): Session Start"
    agent -> qbrain.repoFs "Read AGENTS-README-FIRST.yaml"
    qbrain.repoFs -> agent "API key, base URL, workspace path"
    autolayout lr 200 100
}
dynamic qbrain "MKT-AGENTWORKFLOW-01-part2" "Agent workflow part 2 of 8, Context Retrieval, request and full-text half (agent-workflow.mmd lines 21 to 24)." {
    title "Agent Workflow Sequence (2 of 8): Context Retrieval, full-text half"
    agent -> qbrain.server "POST /qbrainai/context/search" "HTTP REST, X-Api-Key"
    qbrain.server -> qbrain.db "FTS5 + HNSW vector search: FTS5 full-text half"
    qbrain.db -> qbrain.server "Matching code chunks"
    autolayout tb 150 150
}
dynamic qbrain "MKT-AGENTWORKFLOW-01-part3" "Agent workflow part 3 of 8, Context Retrieval, vector half and reply (agent-workflow.mmd lines 23 to 25); the vector half runs with the full-text half of part 2." {
    title "Agent Workflow Sequence (3 of 8): Context Retrieval, vector half and reply"
    qbrain.server -> qbrain.vector "FTS5 + HNSW vector search: HNSW vector half"
    qbrain.vector -> qbrain.server "Matching code chunks"
    qbrain.server -> agent "Ranked context pack" "HTTP REST, X-Api-Key"
    autolayout lr 200 100
}
dynamic qbrain "MKT-AGENTWORKFLOW-01-part4" "Agent workflow part 4 of 8, Task Management, backlog query (agent-workflow.mmd lines 26 to 28)." {
    title "Agent Workflow Sequence (4 of 8): Task Management, backlog query"
    agent -> qbrain.server "GET /qbrainai/todo?section=backlog" "HTTP REST, X-Api-Key"
    qbrain.server -> agent "TODO item list" "HTTP REST, X-Api-Key"
    autolayout lr 200 100
}
dynamic qbrain "MKT-AGENTWORKFLOW-01-part5" "Agent workflow part 5 of 8, Task Management, update and database row (agent-workflow.mmd lines 29 to 30)." {
    title "Agent Workflow Sequence (5 of 8): Task Management, update"
    agent -> qbrain.server "PUT /qbrainai/todo/{id}" "HTTP REST, X-Api-Key"
    qbrain.server -> qbrain.db "Write TODO.yaml / SQLite: the SQLite row"
    autolayout tb 150 150
}
dynamic qbrain "MKT-AGENTWORKFLOW-01-part6" "Agent workflow part 6 of 8, Task Management, projection and reply (agent-workflow.mmd lines 30 to 31); the projection is written with the row of part 5." {
    title "Agent Workflow Sequence (6 of 8): Task Management, projection and reply"
    qbrain.server -> qbrain.repoFs "Write TODO.yaml / SQLite: the TODO.yaml projection"
    qbrain.server -> agent "Updated item" "HTTP REST, X-Api-Key"
    autolayout lr 200 100
}
dynamic qbrain "MKT-AGENTWORKFLOW-01-part7" "Agent workflow part 7 of 8, Session Logging (agent-workflow.mmd lines 32 to 35)." {
    title "Agent Workflow Sequence (7 of 8): Session Logging"
    agent -> qbrain.server "POST /qbrainai/sessionlog" "HTTP REST, X-Api-Key"
    qbrain.server -> qbrain.db "Persist session entry"
    qbrain.server -> agent "Session ID confirmed" "HTTP REST, X-Api-Key"
    autolayout tb 150 150
}
dynamic qbrain "MKT-AGENTWORKFLOW-01-part8" "Agent workflow part 8 of 8, GitHub Sync (agent-workflow.mmd lines 36 to 39)." {
    title "Agent Workflow Sequence (8 of 8): GitHub Sync"
    agent -> qbrain.server "POST /qbrainai/sync" "HTTP REST, X-Api-Key"
    qbrain.server -> github "Sync TODOs with GitHub Issues"
    qbrain.server -> agent "Sync result" "HTTP REST, X-Api-Key"
    autolayout lr 200 100
}

# architecture.mmd (graph, 20 nodes, 31 edges): component views of the MCP Context Server, from the agents through the
# transports and workspace routing to the features and the workspace.
component qbrain.server "MKT-ARCHITECTURE-01" "Architecture part 1 of 7: AI agents and the transport layer (architecture.mmd lines 15 to 25, 44)." {
    title "Architecture (1 of 7): AI agents and the transport layer"
    include agent qbrain.server.stdioHost qbrain.server.mcpHttp qbrain.server.middleware
    exclude "relationship.tag==Planned"
    autolayout tb 150 50
}
component qbrain.server "MKT-ARCHITECTURE-01-part2" "Architecture part 2 of 7: the transports hand requests to the tool surface and the controllers (architecture.mmd lines 22 to 24, 45)." {
    title "Architecture (2 of 7): transports to tools and controllers"
    include qbrain.server.stdioHost qbrain.server.mcpHttp qbrain.server.middleware qbrain.server.mcpTools qbrain.server.api
    exclude "relationship.tag==Planned"
    autolayout tb 150 50
}
component qbrain.server "MKT-ARCHITECTURE-01-part3" "Architecture part 3 of 7: every transport reaches multi-workspace routing (architecture.mmd lines 35, 45)." {
    title "Architecture (3 of 7): multi-workspace routing"
    include qbrain.server.middleware qbrain.server.mcpTools qbrain.server.api qbrain.server.workspace
    exclude "relationship.tag==Planned"
    autolayout tb 150 50
}
component qbrain.server "MKT-ARCHITECTURE-01-part4" "Architecture part 4 of 7: hybrid context search and TODO management (architecture.mmd lines 28, 29, 46, 47)." {
    title "Architecture (4 of 7): context search and TODO management"
    include qbrain.server.api qbrain.server.mcpTools qbrain.server.search qbrain.server.todo qbrain.repoFs
    exclude "relationship.tag==Planned"
    autolayout tb 150 50
}
component qbrain.server "MKT-ARCHITECTURE-01-part5" "Architecture part 5 of 7: session logging and requirements tracking (architecture.mmd lines 30, 33, 46, 47, 49)." {
    title "Architecture (5 of 7): session logging and requirements tracking"
    include qbrain.server.api qbrain.server.mcpTools qbrain.server.sessionLog qbrain.server.requirements qbrain.repoFs
    exclude "relationship.tag==Planned"
    autolayout tb 150 50
}
component qbrain.server "MKT-ARCHITECTURE-01-part6" "Architecture part 6 of 7: GitHub issue sync (architecture.mmd lines 31, 41, 46, 48)." {
    title "Architecture (6 of 7): GitHub issue sync"
    include qbrain.server.api qbrain.server.mcpTools qbrain.server.github github
    exclude "relationship.tag==Planned"
    autolayout tb 150 50
}
component qbrain.server "MKT-ARCHITECTURE-01-part7" "Architecture part 7 of 7: GraphRAG and prompt templates (architecture.mmd lines 32, 34, 46, 49, 50)." {
    title "Architecture (7 of 7): GraphRAG and prompt templates"
    include qbrain.server.api qbrain.server.mcpTools qbrain.server.graphrag qbrain.server.tools qbrain.repoFs
    exclude "relationship.tag==Planned"
    autolayout tb 150 50
}

# ui-tooling.mmd (graph, 7 nodes, 6 edges): the access surfaces around the MCP Context Server, in two parts.
container qbrain "MKT-UITOOLING-01" "UI tooling part 1 of 2: GUI and CLI clients (ui-tooling.mmd lines 15 to 22, 30 to 32)." {
    title "UI Tooling (1 of 2): web, terminal and command-line clients"
    include qbrain.server mktWebUi mktDirector
    exclude "qbrain.server->qbrain.server"
    autolayout tb 150 50
}
component qbrain.libs "MKT-UITOOLING-01-part2" "UI tooling part 2 of 2: client library and agents (ui-tooling.mmd lines 15, 26 to 29, 34, 35)." {
    title "UI Tooling (2 of 2): client library and MCP agents"
    include qbrain.server qbrain.libs.client agent
    exclude "qbrain.server->qbrain.libs.client"
    exclude "relationship.tag==Planned"
    autolayout lr 200 100
}

# features.mmd (mindmap, 25 nodes, 24 edges): custom views of Topic elements, the feature areas first, then one area each.
custom "MKT-FEATURES-01" "Features Overview (1 of 6): QBrainAi feature areas" "Feature map part 1 of 6: root and feature areas (features.mmd lines 15, 16, 21, 26, 31, 35)." {
    include mktFeatures mktFtContext mktFtTasks mktFtCoordination mktFtRequirements mktFtInfrastructure
    autolayout lr 100 50
}
custom "MKT-FEATURES-01-part2" "Features Overview (2 of 6): Context and Search" "Feature map part 2 of 6 (features.mmd lines 16 to 20)." {
    include mktFtContext mktFtHybridSearch mktFtEmbeddings mktFtBm25 mktFtGraphRag
    autolayout lr 100 50
}
custom "MKT-FEATURES-01-part3" "Features Overview (3 of 6): Task Management" "Feature map part 3 of 6 (features.mmd lines 21 to 25)." {
    include mktFtTasks mktFtTodoApi mktFtTodoBackend mktFtIssueSync mktFtTodoFilters
    autolayout lr 100 50
}
custom "MKT-FEATURES-01-part4" "Features Overview (4 of 6): Agent Coordination" "Feature map part 4 of 6 (features.mmd lines 26 to 30)." {
    include mktFtCoordination mktFtSessionLogging mktFtAuditTrail mktFtDecisions mktFtSharedState
    autolayout lr 100 50
}
custom "MKT-FEATURES-01-part5" "Features Overview (5 of 6): Requirements" "Feature map part 5 of 6 (features.mmd lines 31 to 34)." {
    include mktFtRequirements mktFtReqTracking mktFtTraceability mktFtReqGeneration
    autolayout lr 100 50
}
custom "MKT-FEATURES-01-part6" "Features Overview (6 of 6): Infrastructure" "Feature map part 6 of 6 (features.mmd lines 35 to 39)." {
    include mktFtInfrastructure mktFtMultiWorkspace mktFtTransports mktFtTemplates mktFtContainers
    autolayout lr 100 50
}
