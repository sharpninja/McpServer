# Static C4 views (PLAN-C4MODEL-001 D6, AC-5): neighbourhood views of one hub element and at most 3 neighbours,
# so each fits a letter page at 8 to 16 pt; every element and projected relationship appears in at least one view.
# Generated once by ~/.cache/c4-migration/New-C4Views.ps1 (D9) from the exported workspace; edit here from now on.
systemContext qbrain "CTX-QBRAIN-01" "C4 level 1: QBrain.AI and the elements it uses or is used by." {
    title "QBrain.AI: neighbours (1 of 4)"
    include qbrain github llm tunnels
    exclude "element.tag==Planned"
    exclude "relationship.tag==Planned"
    autolayout tb 100 50
}
systemContext qbrain "CTX-QBRAIN-02" "C4 level 1: QBrain.AI and the elements it uses or is used by." {
    title "QBrain.AI: neighbours (2 of 4)"
    include qbrain parseable hf oidc
    exclude "element.tag==Planned"
    exclude "relationship.tag==Planned"
    autolayout tb 100 50
}
systemContext qbrain "CTX-QBRAIN-03" "C4 level 1: QBrain.AI and the elements it uses or is used by." {
    title "QBrain.AI: neighbours (3 of 4)"
    include qbrain peer pluginRepos developer
    exclude "element.tag==Planned"
    exclude "relationship.tag==Planned"
    autolayout tb 100 50
}
systemContext qbrain "CTX-QBRAIN-04" "C4 level 1: QBrain.AI and the elements it uses or is used by." {
    title "QBrain.AI: neighbours (4 of 4)"
    include qbrain agent
    exclude "element.tag==Planned"
    exclude "relationship.tag==Planned"
    autolayout tb 100 50
}
container qbrain "CTR-SERVER-01" "C4 level 2: MCP Context Server and the elements it uses or is used by." {
    title "MCP Context Server: neighbours (1 of 12)"
    include qbrain.server github llm
    exclude "element.tag==Planned"
    exclude "relationship.tag==Planned"
    autolayout tb 100 50
}
container qbrain "CTR-SERVER-02" "C4 level 2: MCP Context Server and the elements it uses or is used by." {
    title "MCP Context Server: neighbours (2 of 12)"
    include qbrain.server tunnels parseable
    exclude "element.tag==Planned"
    exclude "relationship.tag==Planned"
    autolayout tb 100 50
}
container qbrain "CTR-SERVER-03" "C4 level 2: MCP Context Server and the elements it uses or is used by." {
    title "MCP Context Server: neighbours (3 of 12)"
    include qbrain.server hf oidc
    exclude "element.tag==Planned"
    exclude "relationship.tag==Planned"
    autolayout tb 100 50
}
container qbrain "CTR-SERVER-04" "C4 level 2: MCP Context Server and the elements it uses or is used by." {
    title "MCP Context Server: neighbours (4 of 12)"
    include qbrain.server peer qbrain.txDb
    exclude "element.tag==Planned"
    exclude "relationship.tag==Planned"
    autolayout tb 100 50
}
container qbrain "CTR-SERVER-05" "C4 level 2: MCP Context Server and the elements it uses or is used by." {
    title "MCP Context Server: neighbours (5 of 12)"
    include qbrain.server qbrain.db qbrain.vector
    exclude "element.tag==Planned"
    exclude "relationship.tag==Planned"
    autolayout tb 100 50
}
container qbrain "CTR-SERVER-06" "C4 level 2: MCP Context Server and the elements it uses or is used by." {
    title "MCP Context Server: neighbours (6 of 12)"
    include qbrain.server qbrain.repoFs qbrain.subscriber
    exclude "element.tag==Planned"
    exclude "relationship.tag==Planned"
    autolayout tb 100 50
}
container qbrain "CTR-SERVER-07" "C4 level 2: MCP Context Server and the elements it uses or is used by." {
    title "MCP Context Server: neighbours (7 of 12)"
    include qbrain.server qbrain.launcher qbrain.frps
    exclude "element.tag==Planned"
    exclude "relationship.tag==Planned"
    autolayout tb 100 50
}
container qbrain "CTR-SERVER-08" "C4 level 2: MCP Context Server and the elements it uses or is used by." {
    title "MCP Context Server: neighbours (8 of 12)"
    include qbrain.server developer agent
    exclude "element.tag==Planned"
    exclude "relationship.tag==Planned"
    autolayout tb 100 50
}
container qbrain "CTR-SERVER-09" "C4 level 2: MCP Context Server and the elements it uses or is used by." {
    title "MCP Context Server: neighbours (9 of 12)"
    include qbrain.server qbrain.repl qbrain.compat
    exclude "element.tag==Planned"
    exclude "relationship.tag==Planned"
    autolayout tb 100 50
}
container qbrain "CTR-SERVER-10" "C4 level 2: MCP Context Server and the elements it uses or is used by." {
    title "MCP Context Server: neighbours (10 of 12)"
    include qbrain.server qbrain.testSuites qbrain.nukeBuild
    exclude "element.tag==Planned"
    exclude "relationship.tag==Planned"
    autolayout tb 100 50
}
container qbrain "CTR-SERVER-11" "C4 level 2: MCP Context Server and the elements it uses or is used by." {
    title "MCP Context Server: neighbours (11 of 12)"
    include qbrain.server qbrain.devTools qbrain.opsScripts
    exclude "element.tag==Planned"
    exclude "relationship.tag==Planned"
    autolayout tb 100 50
}
container qbrain "CTR-SERVER-12" "C4 level 2: MCP Context Server and the elements it uses or is used by." {
    title "MCP Context Server: neighbours (12 of 12)"
    include qbrain.server qbrain.qbagent qbrain.libs
    exclude "element.tag==Planned"
    exclude "relationship.tag==Planned"
    autolayout tb 100 50
}
container qbrain "CTR-REPOFS-01" "C4 level 2: Workspace Files and the elements it uses or is used by." {
    title "Workspace Files: neighbours (1 of 1)"
    include qbrain.repoFs qbrain.repl qbrain.plugins qbrain.opsScripts
    exclude "element.tag==Planned"
    exclude "relationship.tag==Planned"
    autolayout tb 100 50
}
container qbrain "CTR-KEYSERVER-01" "C4 level 2: Key Server and the elements it uses or is used by." {
    title "Key Server: neighbours (1 of 2)"
    include qbrain.keyserver qbrain.libs qbrain.subscriber
    exclude "element.tag==Planned"
    exclude "relationship.tag==Planned"
    autolayout tb 100 50
}
container qbrain "CTR-KEYSERVER-02" "C4 level 2: Key Server and the elements it uses or is used by." {
    title "Key Server: neighbours (2 of 2)"
    include qbrain.keyserver qbrain.testSuites
    exclude "element.tag==Planned"
    exclude "relationship.tag==Planned"
    autolayout tb 100 50
}
container qbrain "CTR-SUBSCRIBER-01" "C4 level 2: Transaction Subscriber and the elements it uses or is used by." {
    title "Transaction Subscriber: neighbours (1 of 1)"
    include qbrain.subscriber qbrain.testSuites
    exclude "element.tag==Planned"
    exclude "relationship.tag==Planned"
    autolayout tb 100 50
}
container qbrain "CTR-REPL-01" "C4 level 2: REPL Host and the elements it uses or is used by." {
    title "REPL Host: neighbours (1 of 2)"
    include qbrain.repl qbrain.libs developer agent
    exclude "element.tag==Planned"
    exclude "relationship.tag==Planned"
    autolayout tb 100 50
}
container qbrain "CTR-REPL-02" "C4 level 2: REPL Host and the elements it uses or is used by." {
    title "REPL Host: neighbours (2 of 2)"
    include qbrain.repl qbrain.testSuites qbrain.devTools
    exclude "element.tag==Planned"
    exclude "relationship.tag==Planned"
    autolayout tb 100 50
}
container qbrain "CTR-PLUGINS-01" "C4 level 2: Agent Plugin Core and the elements it uses or is used by." {
    title "Agent Plugin Core: neighbours (1 of 1)"
    include qbrain.plugins pluginRepos qbrain.devTools agent
    exclude "element.tag==Planned"
    exclude "relationship.tag==Planned"
    autolayout tb 100 50
}
container qbrain "CTR-LIBS-01" "C4 level 2: Shared Libraries and the elements it uses or is used by." {
    title "Shared Libraries: neighbours (1 of 1)"
    include qbrain.libs qbrain.compat qbrain.sampleHost qbrain.testSuites
    exclude "element.tag==Planned"
    exclude "relationship.tag==Planned"
    autolayout tb 100 50
}
container qbrain "CTR-COMPAT-01" "C4 level 2: Compatibility Packages and the elements it uses or is used by." {
    title "Compatibility Packages: neighbours (1 of 1)"
    include qbrain.compat qbrain.qbagent
    exclude "element.tag==Planned"
    exclude "relationship.tag==Planned"
    autolayout tb 100 50
}
container qbrain "CTR-SAMPLEHOST-01" "C4 level 2: McpAgent Sample Host and the elements it uses or is used by." {
    title "McpAgent Sample Host: neighbours (1 of 1)"
    include qbrain.sampleHost llm
    exclude "element.tag==Planned"
    exclude "relationship.tag==Planned"
    autolayout tb 100 50
}
container qbrain "CTR-TESTSUITES-01" "C4 level 2: Test Suites and the elements it uses or is used by." {
    title "Test Suites: neighbours (1 of 1)"
    include qbrain.testSuites llm qbrain.qbagent qbrain.launcher
    exclude "element.tag==Planned"
    exclude "relationship.tag==Planned"
    autolayout tb 100 50
}
container qbrain "CTR-NUKEBUILD-01" "C4 level 2: Nuke Build and the elements it uses or is used by." {
    title "Nuke Build: neighbours (1 of 1)"
    include qbrain.nukeBuild qbrain.devTools
    exclude "element.tag==Planned"
    exclude "relationship.tag==Planned"
    autolayout tb 100 50
}
container qbrain "CTR-OPSSCRIPTS-01" "C4 level 2: Operations Scripts and the elements it uses or is used by." {
    title "Operations Scripts: neighbours (1 of 1)"
    include qbrain.opsScripts oidc
    exclude "element.tag==Planned"
    exclude "relationship.tag==Planned"
    autolayout tb 100 50
}
component qbrain.server "CMP-SERVER-API-01" "C4 level 3, MCP Context Server: REST Controllers and the elements it uses or is used by." {
    title "REST Controllers: neighbours (1 of 9)"
    include qbrain.server.api qbrain.server.pairing qbrain.server.workspace
    exclude "element.tag==Planned"
    exclude "relationship.tag==Planned"
    autolayout tb 100 50
}
component qbrain.server "CMP-SERVER-API-02" "C4 level 3, MCP Context Server: REST Controllers and the elements it uses or is used by." {
    title "REST Controllers: neighbours (2 of 9)"
    include qbrain.server.api qbrain.server.todo qbrain.server.sessionLog
    exclude "element.tag==Planned"
    exclude "relationship.tag==Planned"
    autolayout tb 100 50
}
component qbrain.server "CMP-SERVER-API-03" "C4 level 3, MCP Context Server: REST Controllers and the elements it uses or is used by." {
    title "REST Controllers: neighbours (3 of 9)"
    include qbrain.server.api qbrain.server.requirements qbrain.server.memory
    exclude "element.tag==Planned"
    exclude "relationship.tag==Planned"
    autolayout tb 100 50
}
component qbrain.server "CMP-SERVER-API-04" "C4 level 3, MCP Context Server: REST Controllers and the elements it uses or is used by." {
    title "REST Controllers: neighbours (4 of 9)"
    include qbrain.server.api qbrain.server.search qbrain.server.graphrag
    exclude "element.tag==Planned"
    exclude "relationship.tag==Planned"
    autolayout tb 100 50
}
component qbrain.server "CMP-SERVER-API-05" "C4 level 3, MCP Context Server: REST Controllers and the elements it uses or is used by." {
    title "REST Controllers: neighbours (5 of 9)"
    include qbrain.server.api qbrain.server.triage qbrain.server.brain
    exclude "element.tag==Planned"
    exclude "relationship.tag==Planned"
    autolayout tb 100 50
}
component qbrain.server "CMP-SERVER-API-06" "C4 level 3, MCP Context Server: REST Controllers and the elements it uses or is used by." {
    title "REST Controllers: neighbours (6 of 9)"
    include qbrain.server.api qbrain.server.agents qbrain.server.federation
    exclude "element.tag==Planned"
    exclude "relationship.tag==Planned"
    autolayout tb 100 50
}
component qbrain.server "CMP-SERVER-API-07" "C4 level 3, MCP Context Server: REST Controllers and the elements it uses or is used by." {
    title "REST Controllers: neighbours (7 of 9)"
    include qbrain.server.api qbrain.server.tunnel qbrain.server.github
    exclude "element.tag==Planned"
    exclude "relationship.tag==Planned"
    autolayout tb 100 50
}
component qbrain.server "CMP-SERVER-API-08" "C4 level 3, MCP Context Server: REST Controllers and the elements it uses or is used by." {
    title "REST Controllers: neighbours (8 of 9)"
    include qbrain.server.api qbrain.server.txsec qbrain.server.tools
    exclude "element.tag==Planned"
    exclude "relationship.tag==Planned"
    autolayout tb 100 50
}
component qbrain.server "CMP-SERVER-API-09" "C4 level 3, MCP Context Server: REST Controllers and the elements it uses or is used by." {
    title "REST Controllers: neighbours (9 of 9)"
    include qbrain.server.api qbrain.server.storage qbrain.server.middleware
    exclude "element.tag==Planned"
    exclude "relationship.tag==Planned"
    autolayout tb 100 50
}
component qbrain.server "CMP-SERVER-MCPHTTP-01" "C4 level 3, MCP Context Server: MCP HTTP Transport and the elements it uses or is used by." {
    title "MCP HTTP Transport: neighbours (1 of 1)"
    include qbrain.server.mcpHttp qbrain.server.mcpTools agent
    exclude "element.tag==Planned"
    exclude "relationship.tag==Planned"
    autolayout tb 100 50
}
component qbrain.server "CMP-SERVER-STDIOHOST-01" "C4 level 3, MCP Context Server: MCP STDIO Host and the elements it uses or is used by." {
    title "MCP STDIO Host: neighbours (1 of 1)"
    include qbrain.server.stdioHost qbrain.server.mcpTools agent
    exclude "element.tag==Planned"
    exclude "relationship.tag==Planned"
    autolayout tb 100 50
}
component qbrain.server "CMP-SERVER-MCPTOOLS-01" "C4 level 3, MCP Context Server: MCP Tool Surface and the elements it uses or is used by." {
    title "MCP Tool Surface: neighbours (1 of 5)"
    include qbrain.server.mcpTools qbrain.server.workspace qbrain.server.todo qbrain.server.sessionLog
    exclude "element.tag==Planned"
    exclude "relationship.tag==Planned"
    autolayout tb 100 50
}
component qbrain.server "CMP-SERVER-MCPTOOLS-02" "C4 level 3, MCP Context Server: MCP Tool Surface and the elements it uses or is used by." {
    title "MCP Tool Surface: neighbours (2 of 5)"
    include qbrain.server.mcpTools qbrain.server.requirements qbrain.server.memory qbrain.server.search
    exclude "element.tag==Planned"
    exclude "relationship.tag==Planned"
    autolayout tb 100 50
}
component qbrain.server "CMP-SERVER-MCPTOOLS-03" "C4 level 3, MCP Context Server: MCP Tool Surface and the elements it uses or is used by." {
    title "MCP Tool Surface: neighbours (3 of 5)"
    include qbrain.server.mcpTools qbrain.server.graphrag qbrain.server.triage qbrain.server.agents
    exclude "element.tag==Planned"
    exclude "relationship.tag==Planned"
    autolayout tb 100 50
}
component qbrain.server "CMP-SERVER-MCPTOOLS-04" "C4 level 3, MCP Context Server: MCP Tool Surface and the elements it uses or is used by." {
    title "MCP Tool Surface: neighbours (4 of 5)"
    include qbrain.server.mcpTools qbrain.server.github qbrain.server.txsec qbrain.server.tools
    exclude "element.tag==Planned"
    exclude "relationship.tag==Planned"
    autolayout tb 100 50
}
component qbrain.server "CMP-SERVER-MCPTOOLS-05" "C4 level 3, MCP Context Server: MCP Tool Surface and the elements it uses or is used by." {
    title "MCP Tool Surface: neighbours (5 of 5)"
    include qbrain.server.mcpTools qbrain.server.storage
    exclude "element.tag==Planned"
    exclude "relationship.tag==Planned"
    autolayout tb 100 50
}
component qbrain.server "CMP-SERVER-PAIRING-01" "C4 level 3, MCP Context Server: Pairing and API Key and the elements it uses or is used by." {
    title "Pairing and API Key: neighbours (1 of 1)"
    include qbrain.server.pairing qbrain.server.tools qbrain.server.middleware qbrain.server.agents
    exclude "element.tag==Planned"
    exclude "relationship.tag==Planned"
    autolayout tb 100 50
}
component qbrain.server "CMP-SERVER-MIDDLEWARE-01" "C4 level 3, MCP Context Server: Request Pipeline and the elements it uses or is used by." {
    title "Request Pipeline: neighbours (1 of 2)"
    include qbrain.server.middleware qbrain.server.identity qbrain.server.workspace qbrain.server.federation
    exclude "element.tag==Planned"
    exclude "relationship.tag==Planned"
    autolayout tb 100 50
}
component qbrain.server "CMP-SERVER-MIDDLEWARE-02" "C4 level 3, MCP Context Server: Request Pipeline and the elements it uses or is used by." {
    title "Request Pipeline: neighbours (2 of 2)"
    include qbrain.server.middleware qbrain.libs
    exclude "element.tag==Planned"
    exclude "relationship.tag==Planned"
    autolayout tb 100 50
}
component qbrain.server "CMP-SERVER-IDENTITY-01" "C4 level 3, MCP Context Server: Identity and the elements it uses or is used by." {
    title "Identity: neighbours (1 of 2)"
    include qbrain.server.identity oidc qbrain.server.identityDbContext
    exclude "element.tag==Planned"
    exclude "relationship.tag==Planned"
    autolayout tb 100 50
}
component qbrain.server "CMP-SERVER-IDENTITY-02" "C4 level 3, MCP Context Server: Identity and the elements it uses or is used by." {
    title "Identity: neighbours (2 of 2)"
    include qbrain.server.identity qbrain.db
    exclude "element.tag==Planned"
    exclude "relationship.tag==Planned"
    autolayout tb 100 50
}
component qbrain.server "CMP-SERVER-WORKSPACE-01" "C4 level 3, MCP Context Server: Workspace Service and the elements it uses or is used by." {
    title "Workspace Service: neighbours (1 of 7)"
    include qbrain.server.workspace qbrain.server.servicesLib qbrain.repoFs
    exclude "element.tag==Planned"
    exclude "relationship.tag==Planned"
    autolayout tb 100 50
}
component qbrain.server "CMP-SERVER-WORKSPACE-02" "C4 level 3, MCP Context Server: Workspace Service and the elements it uses or is used by." {
    title "Workspace Service: neighbours (2 of 7)"
    include qbrain.server.workspace qbrain.server.requirements qbrain.server.search
    exclude "element.tag==Planned"
    exclude "relationship.tag==Planned"
    autolayout tb 100 50
}
component qbrain.server "CMP-SERVER-WORKSPACE-03" "C4 level 3, MCP Context Server: Workspace Service and the elements it uses or is used by." {
    title "Workspace Service: neighbours (3 of 7)"
    include qbrain.server.workspace qbrain.server.graphrag qbrain.server.triage
    exclude "element.tag==Planned"
    exclude "relationship.tag==Planned"
    autolayout tb 100 50
}
component qbrain.server "CMP-SERVER-WORKSPACE-04" "C4 level 3, MCP Context Server: Workspace Service and the elements it uses or is used by." {
    title "Workspace Service: neighbours (4 of 7)"
    include qbrain.server.workspace qbrain.server.brain qbrain.server.agents
    exclude "element.tag==Planned"
    exclude "relationship.tag==Planned"
    autolayout tb 100 50
}
component qbrain.server "CMP-SERVER-WORKSPACE-05" "C4 level 3, MCP Context Server: Workspace Service and the elements it uses or is used by." {
    title "Workspace Service: neighbours (5 of 7)"
    include qbrain.server.workspace qbrain.server.tunnel qbrain.server.github
    exclude "element.tag==Planned"
    exclude "relationship.tag==Planned"
    autolayout tb 100 50
}
component qbrain.server "CMP-SERVER-WORKSPACE-06" "C4 level 3, MCP Context Server: Workspace Service and the elements it uses or is used by." {
    title "Workspace Service: neighbours (6 of 7)"
    include qbrain.server.workspace qbrain.server.txsec qbrain.server.mcpDbContext
    exclude "element.tag==Planned"
    exclude "relationship.tag==Planned"
    autolayout tb 100 50
}
component qbrain.server "CMP-SERVER-WORKSPACE-07" "C4 level 3, MCP Context Server: Workspace Service and the elements it uses or is used by." {
    title "Workspace Service: neighbours (7 of 7)"
    include qbrain.server.workspace qbrain.server.storage
    exclude "element.tag==Planned"
    exclude "relationship.tag==Planned"
    autolayout tb 100 50
}
component qbrain.server "CMP-SERVER-TODO-01" "C4 level 3, MCP Context Server: TODO Service and the elements it uses or is used by." {
    title "TODO Service: neighbours (1 of 6)"
    include qbrain.server.todo qbrain.server.requirements qbrain.server.github
    exclude "element.tag==Planned"
    exclude "relationship.tag==Planned"
    autolayout tb 100 50
}
component qbrain.server "CMP-SERVER-TODO-02" "C4 level 3, MCP Context Server: TODO Service and the elements it uses or is used by." {
    title "TODO Service: neighbours (2 of 6)"
    include qbrain.server.todo qbrain.server.tools qbrain.server.storage
    exclude "element.tag==Planned"
    exclude "relationship.tag==Planned"
    autolayout tb 100 50
}
component qbrain.server "CMP-SERVER-TODO-03" "C4 level 3, MCP Context Server: TODO Service and the elements it uses or is used by." {
    title "TODO Service: neighbours (3 of 6)"
    include qbrain.server.todo qbrain.server.servicesLib qbrain.repoFs
    exclude "element.tag==Planned"
    exclude "relationship.tag==Planned"
    autolayout tb 100 50
}
component qbrain.server "CMP-SERVER-TODO-04" "C4 level 3, MCP Context Server: TODO Service and the elements it uses or is used by." {
    title "TODO Service: neighbours (4 of 6)"
    include qbrain.server.todo qbrain.server.triage qbrain.server.brain
    exclude "element.tag==Planned"
    exclude "relationship.tag==Planned"
    autolayout tb 100 50
}
component qbrain.server "CMP-SERVER-TODO-05" "C4 level 3, MCP Context Server: TODO Service and the elements it uses or is used by." {
    title "TODO Service: neighbours (5 of 6)"
    include qbrain.server.todo qbrain.server.agents qbrain.server.txsec
    exclude "element.tag==Planned"
    exclude "relationship.tag==Planned"
    autolayout tb 100 50
}
component qbrain.server "CMP-SERVER-TODO-06" "C4 level 3, MCP Context Server: TODO Service and the elements it uses or is used by." {
    title "TODO Service: neighbours (6 of 6)"
    include qbrain.server.todo qbrain.server.federation
    exclude "element.tag==Planned"
    exclude "relationship.tag==Planned"
    autolayout tb 100 50
}
component qbrain.server "CMP-SERVER-SESSIONLOG-01" "C4 level 3, MCP Context Server: Session Log Service and the elements it uses or is used by." {
    title "Session Log Service: neighbours (1 of 5)"
    include qbrain.server.sessionLog qbrain.server.storage qbrain.server.servicesLib
    exclude "element.tag==Planned"
    exclude "relationship.tag==Planned"
    autolayout tb 100 50
}
component qbrain.server "CMP-SERVER-SESSIONLOG-02" "C4 level 3, MCP Context Server: Session Log Service and the elements it uses or is used by." {
    title "Session Log Service: neighbours (2 of 5)"
    include qbrain.server.sessionLog qbrain.server.transcriptsLib qbrain.repoFs
    exclude "element.tag==Planned"
    exclude "relationship.tag==Planned"
    autolayout tb 100 50
}
component qbrain.server "CMP-SERVER-SESSIONLOG-03" "C4 level 3, MCP Context Server: Session Log Service and the elements it uses or is used by." {
    title "Session Log Service: neighbours (3 of 5)"
    include qbrain.server.sessionLog qbrain.server.search qbrain.server.triage
    exclude "element.tag==Planned"
    exclude "relationship.tag==Planned"
    autolayout tb 100 50
}
component qbrain.server "CMP-SERVER-SESSIONLOG-04" "C4 level 3, MCP Context Server: Session Log Service and the elements it uses or is used by." {
    title "Session Log Service: neighbours (4 of 5)"
    include qbrain.server.sessionLog qbrain.server.brain qbrain.server.txsec
    exclude "element.tag==Planned"
    exclude "relationship.tag==Planned"
    autolayout tb 100 50
}
component qbrain.server "CMP-SERVER-SESSIONLOG-05" "C4 level 3, MCP Context Server: Session Log Service and the elements it uses or is used by." {
    title "Session Log Service: neighbours (5 of 5)"
    include qbrain.server.sessionLog qbrain.server.federation
    exclude "element.tag==Planned"
    exclude "relationship.tag==Planned"
    autolayout tb 100 50
}
component qbrain.server "CMP-SERVER-REQUIREMENTS-01" "C4 level 3, MCP Context Server: Requirements and Use Cases and the elements it uses or is used by." {
    title "Requirements and Use Cases: neighbours (1 of 2)"
    include qbrain.server.requirements qbrain.server.storage qbrain.server.servicesLib
    exclude "element.tag==Planned"
    exclude "relationship.tag==Planned"
    autolayout tb 100 50
}
component qbrain.server "CMP-SERVER-REQUIREMENTS-02" "C4 level 3, MCP Context Server: Requirements and Use Cases and the elements it uses or is used by." {
    title "Requirements and Use Cases: neighbours (2 of 2)"
    include qbrain.server.requirements qbrain.server.brain qbrain.server.txsec
    exclude "element.tag==Planned"
    exclude "relationship.tag==Planned"
    autolayout tb 100 50
}
component qbrain.server "CMP-SERVER-MEMORY-01" "C4 level 3, MCP Context Server: Memory Service and the elements it uses or is used by." {
    title "Memory Service: neighbours (1 of 1)"
    include qbrain.server.memory qbrain.server.storage qbrain.server.servicesLib qbrain.server.txsec
    exclude "element.tag==Planned"
    exclude "relationship.tag==Planned"
    autolayout tb 100 50
}
component qbrain.server "CMP-SERVER-SEARCH-01" "C4 level 3, MCP Context Server: Context Search and Ingestion and the elements it uses or is used by." {
    title "Context Search and Ingestion: neighbours (1 of 5)"
    include qbrain.server.search hf qbrain.server.github
    exclude "element.tag==Planned"
    exclude "relationship.tag==Planned"
    autolayout tb 100 50
}
component qbrain.server "CMP-SERVER-SEARCH-02" "C4 level 3, MCP Context Server: Context Search and Ingestion and the elements it uses or is used by." {
    title "Context Search and Ingestion: neighbours (2 of 5)"
    include qbrain.server.search qbrain.server.storage qbrain.server.servicesLib
    exclude "element.tag==Planned"
    exclude "relationship.tag==Planned"
    autolayout tb 100 50
}
component qbrain.server "CMP-SERVER-SEARCH-03" "C4 level 3, MCP Context Server: Context Search and Ingestion and the elements it uses or is used by." {
    title "Context Search and Ingestion: neighbours (3 of 5)"
    include qbrain.server.search qbrain.server.storageLib qbrain.vector
    exclude "element.tag==Planned"
    exclude "relationship.tag==Planned"
    autolayout tb 100 50
}
component qbrain.server "CMP-SERVER-SEARCH-04" "C4 level 3, MCP Context Server: Context Search and Ingestion and the elements it uses or is used by." {
    title "Context Search and Ingestion: neighbours (4 of 5)"
    include qbrain.server.search qbrain.repoFs qbrain.server.brain
    exclude "element.tag==Planned"
    exclude "relationship.tag==Planned"
    autolayout tb 100 50
}
component qbrain.server "CMP-SERVER-SEARCH-05" "C4 level 3, MCP Context Server: Context Search and Ingestion and the elements it uses or is used by." {
    title "Context Search and Ingestion: neighbours (5 of 5)"
    include qbrain.server.search qbrain.server.txsec
    exclude "element.tag==Planned"
    exclude "relationship.tag==Planned"
    autolayout tb 100 50
}
component qbrain.server "CMP-SERVER-GRAPHRAG-01" "C4 level 3, MCP Context Server: GraphRAG and the elements it uses or is used by." {
    title "GraphRAG: neighbours (1 of 3)"
    include qbrain.server.graphrag qbrain.server.federation qbrain.server.storage qbrain.server.servicesLib
    exclude "element.tag==Planned"
    exclude "relationship.tag==Planned"
    autolayout tb 100 50
}
component qbrain.server "CMP-SERVER-GRAPHRAG-02" "C4 level 3, MCP Context Server: GraphRAG and the elements it uses or is used by." {
    title "GraphRAG: neighbours (2 of 3)"
    include qbrain.server.graphrag qbrain.server.storageLib qbrain.server.cqrsLib qbrain.libs
    exclude "element.tag==Planned"
    exclude "relationship.tag==Planned"
    autolayout tb 100 50
}
component qbrain.server "CMP-SERVER-GRAPHRAG-03" "C4 level 3, MCP Context Server: GraphRAG and the elements it uses or is used by." {
    title "GraphRAG: neighbours (3 of 3)"
    include qbrain.server.graphrag qbrain.server.brain qbrain.server.txsec
    exclude "element.tag==Planned"
    exclude "relationship.tag==Planned"
    autolayout tb 100 50
}
component qbrain.server "CMP-SERVER-TRIAGE-01" "C4 level 3, MCP Context Server: Triage, Handoff and Hostile Review and the elements it uses or is used by." {
    title "Triage, Handoff and Hostile Review: neighbours (1 of 1)"
    include qbrain.server.triage qbrain.server.tools qbrain.server.storage qbrain.server.servicesLib
    exclude "element.tag==Planned"
    exclude "relationship.tag==Planned"
    autolayout tb 100 50
}
component qbrain.server "CMP-SERVER-BRAIN-01" "C4 level 3, MCP Context Server: Brain Slots and Quad Brain and the elements it uses or is used by." {
    title "Brain Slots and Quad Brain: neighbours (1 of 1)"
    include qbrain.server.brain llm qbrain.server.storage
    exclude "element.tag==Planned"
    exclude "relationship.tag==Planned"
    autolayout tb 100 50
}
component qbrain.server "CMP-SERVER-AGENTS-01" "C4 level 3, MCP Context Server: Agent Pool and Processes and the elements it uses or is used by." {
    title "Agent Pool and Processes: neighbours (1 of 2)"
    include qbrain.server.agents llm qbrain.server.storage
    exclude "element.tag==Planned"
    exclude "relationship.tag==Planned"
    autolayout tb 100 50
}
component qbrain.server "CMP-SERVER-AGENTS-02" "C4 level 3, MCP Context Server: Agent Pool and Processes and the elements it uses or is used by." {
    title "Agent Pool and Processes: neighbours (2 of 2)"
    include qbrain.server.agents qbrain.server.servicesLib qbrain.launcher
    exclude "element.tag==Planned"
    exclude "relationship.tag==Planned"
    autolayout tb 100 50
}
component qbrain.server "CMP-SERVER-FEDERATION-01" "C4 level 3, MCP Context Server: Federation and the elements it uses or is used by." {
    title "Federation: neighbours (1 of 1)"
    include qbrain.server.federation peer qbrain.server.txsec
    exclude "element.tag==Planned"
    exclude "relationship.tag==Planned"
    autolayout tb 100 50
}
component qbrain.server "CMP-SERVER-TUNNEL-01" "C4 level 3, MCP Context Server: Tunnel Registry and the elements it uses or is used by." {
    title "Tunnel Registry: neighbours (1 of 2)"
    include qbrain.server.tunnel tunnels qbrain.server.servicesLib
    exclude "element.tag==Planned"
    exclude "relationship.tag==Planned"
    autolayout tb 100 50
}
component qbrain.server "CMP-SERVER-TUNNEL-02" "C4 level 3, MCP Context Server: Tunnel Registry and the elements it uses or is used by." {
    title "Tunnel Registry: neighbours (2 of 2)"
    include qbrain.server.tunnel qbrain.frps
    exclude "element.tag==Planned"
    exclude "relationship.tag==Planned"
    autolayout tb 100 50
}
component qbrain.server "CMP-SERVER-GITHUB-01" "C4 level 3, MCP Context Server: GitHub Integration and the elements it uses or is used by." {
    title "GitHub Integration: neighbours (1 of 1)"
    include qbrain.server.github github qbrain.server.servicesLib
    exclude "element.tag==Planned"
    exclude "relationship.tag==Planned"
    autolayout tb 100 50
}
component qbrain.server "CMP-SERVER-TXSEC-01" "C4 level 3, MCP Context Server: Transaction Security and the elements it uses or is used by." {
    title "Transaction Security: neighbours (1 of 1)"
    include qbrain.server.txsec qbrain.server.txDbContext qbrain.subscriber qbrain.libs
    exclude "element.tag==Planned"
    exclude "relationship.tag==Planned"
    autolayout tb 100 50
}
component qbrain.server "CMP-SERVER-STORAGE-01" "C4 level 3, MCP Context Server: Storage and Providers and the elements it uses or is used by." {
    title "Storage and Providers: neighbours (1 of 2)"
    include qbrain.server.storage qbrain.server.mcpDbContext qbrain.server.storageLib
    exclude "element.tag==Planned"
    exclude "relationship.tag==Planned"
    autolayout tb 100 50
}
component qbrain.server "CMP-SERVER-STORAGE-02" "C4 level 3, MCP Context Server: Storage and Providers and the elements it uses or is used by." {
    title "Storage and Providers: neighbours (2 of 2)"
    include qbrain.server.storage qbrain.db
    exclude "element.tag==Planned"
    exclude "relationship.tag==Planned"
    autolayout tb 100 50
}
component qbrain.server "CMP-SERVER-MCPDBCONTEXT-01" "C4 level 3, MCP Context Server: McpDbContext and the elements it uses or is used by." {
    title "McpDbContext: neighbours (1 of 2)"
    include qbrain.server.mcpDbContext qbrain.db qbrain.server.sqliteMigrations qbrain.server.sqlServerMigrations
    exclude "element.tag==Planned"
    exclude "relationship.tag==Planned"
    autolayout tb 100 50
}
component qbrain.server "CMP-SERVER-MCPDBCONTEXT-02" "C4 level 3, MCP Context Server: McpDbContext and the elements it uses or is used by." {
    title "McpDbContext: neighbours (2 of 2)"
    include qbrain.server.mcpDbContext qbrain.server.postgresMigrations
    exclude "element.tag==Planned"
    exclude "relationship.tag==Planned"
    autolayout tb 100 50
}
component qbrain.server "CMP-SERVER-IDENTITYDBCONTEXT-01" "C4 level 3, MCP Context Server: McpIdentityDbContext and the elements it uses or is used by." {
    title "McpIdentityDbContext: neighbours (1 of 1)"
    include qbrain.server.identityDbContext qbrain.db
    exclude "element.tag==Planned"
    exclude "relationship.tag==Planned"
    autolayout tb 100 50
}
component qbrain.server "CMP-SERVER-TXDBCONTEXT-01" "C4 level 3, MCP Context Server: TransactionSecurityDbContext and the elements it uses or is used by." {
    title "TransactionSecurityDbContext: neighbours (1 of 1)"
    include qbrain.server.txDbContext qbrain.txDb
    exclude "element.tag==Planned"
    exclude "relationship.tag==Planned"
    autolayout tb 100 50
}
component qbrain.server "CMP-SERVER-SQLITEMIGRATIONS-01" "C4 level 3, MCP Context Server: QBrainAi.Storage.SqliteMigrations and the elements it uses or is used by." {
    title "QBrainAi.Storage.SqliteMigrations: neighbours (1 of 1)"
    include qbrain.server.sqliteMigrations qbrain.server.storageLib
    exclude "element.tag==Planned"
    exclude "relationship.tag==Planned"
    autolayout tb 100 50
}
component qbrain.server "CMP-SERVER-SQLSERVERMIGRATIONS-01" "C4 level 3, MCP Context Server: QBrainAi.Storage.SqlServerMigrations and the elements it uses or is used by." {
    title "QBrainAi.Storage.SqlServerMigrations: neighbours (1 of 1)"
    include qbrain.server.sqlServerMigrations qbrain.server.storageLib
    exclude "element.tag==Planned"
    exclude "relationship.tag==Planned"
    autolayout tb 100 50
}
component qbrain.server "CMP-SERVER-POSTGRESMIGRATIONS-01" "C4 level 3, MCP Context Server: QBrainAi.Storage.PostgreSqlMigrations and the elements it uses or is used by." {
    title "QBrainAi.Storage.PostgreSqlMigrations: neighbours (1 of 1)"
    include qbrain.server.postgresMigrations qbrain.server.storageLib qbrain.db
    exclude "element.tag==Planned"
    exclude "relationship.tag==Planned"
    autolayout tb 100 50
}
component qbrain.server "CMP-SERVER-SERVICESLIB-01" "C4 level 3, MCP Context Server: QBrainAi.Services and the elements it uses or is used by." {
    title "QBrainAi.Services: neighbours (1 of 3)"
    include qbrain.server.servicesLib qbrain.server.storageLib qbrain.server.cqrsLib
    exclude "element.tag==Planned"
    exclude "relationship.tag==Planned"
    autolayout tb 100 50
}
component qbrain.server "CMP-SERVER-SERVICESLIB-02" "C4 level 3, MCP Context Server: QBrainAi.Services and the elements it uses or is used by." {
    title "QBrainAi.Services: neighbours (2 of 3)"
    include qbrain.server.servicesLib qbrain.server.cqrsMvvmLib qbrain.libs
    exclude "element.tag==Planned"
    exclude "relationship.tag==Planned"
    autolayout tb 100 50
}
component qbrain.server "CMP-SERVER-SERVICESLIB-03" "C4 level 3, MCP Context Server: QBrainAi.Services and the elements it uses or is used by." {
    title "QBrainAi.Services: neighbours (3 of 3)"
    include qbrain.server.servicesLib qbrain.devTools
    exclude "element.tag==Planned"
    exclude "relationship.tag==Planned"
    autolayout tb 100 50
}
component qbrain.server "CMP-SERVER-CQRSLIB-01" "C4 level 3, MCP Context Server: QBrainAi.Cqrs and the elements it uses or is used by." {
    title "QBrainAi.Cqrs: neighbours (1 of 1)"
    include qbrain.server.cqrsLib qbrain.server.cqrsMvvmLib qbrain.compat qbrain.testSuites
    exclude "element.tag==Planned"
    exclude "relationship.tag==Planned"
    autolayout tb 100 50
}
component qbrain.keyserver "CMP-KEYSERVER-KSAPI-01" "C4 level 3, Key Server: Key Server Endpoints and the elements it uses or is used by." {
    title "Key Server Endpoints: neighbours (1 of 1)"
    include qbrain.keyserver.ksApi qbrain.keyserver.ksRegistry
    exclude "element.tag==Planned"
    exclude "relationship.tag==Planned"
    autolayout tb 100 50
}
component qbrain.keyserver "CMP-KEYSERVER-KSREGISTRY-01" "C4 level 3, Key Server: Party Registry and Manifest Service and the elements it uses or is used by." {
    title "Party Registry and Manifest Service: neighbours (1 of 1)"
    include qbrain.keyserver.ksRegistry qbrain.libs
    exclude "element.tag==Planned"
    exclude "relationship.tag==Planned"
    autolayout tb 100 50
}
component qbrain.repl "CMP-REPL-STDIO-01" "C4 level 3, REPL Host: Agent STDIO Handler and the elements it uses or is used by." {
    title "Agent STDIO Handler: neighbours (1 of 1)"
    include qbrain.repl.stdio qbrain.repl.dispatcher agent
    exclude "element.tag==Planned"
    exclude "relationship.tag==Planned"
    autolayout tb 100 50
}
component qbrain.repl "CMP-REPL-DISPATCHER-01" "C4 level 3, REPL Host: REPL Command Dispatcher and the elements it uses or is used by." {
    title "REPL Command Dispatcher: neighbours (1 of 1)"
    include qbrain.repl.dispatcher qbrain.repl.workflows qbrain.libs
    exclude "element.tag==Planned"
    exclude "relationship.tag==Planned"
    autolayout tb 100 50
}
component qbrain.repl "CMP-REPL-TRUST-01" "C4 level 3, REPL Host: Marker and Trust Bootstrap and the elements it uses or is used by." {
    title "Marker and Trust Bootstrap: neighbours (1 of 1)"
    include qbrain.repl.trust qbrain.repoFs qbrain.libs
    exclude "element.tag==Planned"
    exclude "relationship.tag==Planned"
    autolayout tb 100 50
}
component qbrain.plugins "CMP-PLUGINS-PLUGINPS-01" "C4 level 3, Agent Plugin Core: lib-ps and the elements it uses or is used by." {
    title "lib-ps: neighbours (1 of 2)"
    include qbrain.plugins.pluginPs qbrain.repl qbrain.devTools
    exclude "element.tag==Planned"
    exclude "relationship.tag==Planned"
    autolayout tb 100 50
}
component qbrain.plugins "CMP-PLUGINS-PLUGINPS-02" "C4 level 3, Agent Plugin Core: lib-ps and the elements it uses or is used by." {
    title "lib-ps: neighbours (2 of 2)"
    include qbrain.plugins.pluginPs qbrain.plugins.pluginHosts
    exclude "element.tag==Planned"
    exclude "relationship.tag==Planned"
    autolayout tb 100 50
}
component qbrain.plugins "CMP-PLUGINS-PLUGINNODE-01" "C4 level 3, Agent Plugin Core: lib-node and the elements it uses or is used by." {
    title "lib-node: neighbours (1 of 1)"
    include qbrain.plugins.pluginNode qbrain.devTools qbrain.plugins.pluginHosts
    exclude "element.tag==Planned"
    exclude "relationship.tag==Planned"
    autolayout tb 100 50
}
component qbrain.plugins "CMP-PLUGINS-PLUGINSH-01" "C4 level 3, Agent Plugin Core: lib-sh and the elements it uses or is used by." {
    title "lib-sh: neighbours (1 of 1)"
    include qbrain.plugins.pluginSh qbrain.plugins.pluginHosts
    exclude "element.tag==Planned"
    exclude "relationship.tag==Planned"
    autolayout tb 100 50
}
component qbrain.plugins "CMP-PLUGINS-PLUGINHOSTS-01" "C4 level 3, Agent Plugin Core: Host adapters and the elements it uses or is used by." {
    title "Host adapters: neighbours (1 of 1)"
    include qbrain.plugins.pluginHosts agent
    exclude "element.tag==Planned"
    exclude "relationship.tag==Planned"
    autolayout tb 100 50
}
component qbrain.plugins "CMP-PLUGINS-PLUGINHOOKS-01" "C4 level 3, Agent Plugin Core: Hook templates and skills and the elements it uses or is used by." {
    title "Hook templates and skills: neighbours (1 of 1)"
    include qbrain.plugins.pluginHooks qbrain.plugins.pluginSync
    exclude "element.tag==Planned"
    exclude "relationship.tag==Planned"
    autolayout tb 100 50
}
component qbrain.plugins "CMP-PLUGINS-PLUGINSYNC-01" "C4 level 3, Agent Plugin Core: Plugin sync and the elements it uses or is used by." {
    title "Plugin sync: neighbours (1 of 1)"
    include qbrain.plugins.pluginSync pluginRepos
    exclude "element.tag==Planned"
    exclude "relationship.tag==Planned"
    autolayout tb 100 50
}
component qbrain.plugins "CMP-PLUGINS-AGENTCOREPACKAGE-01" "C4 level 3, Agent Plugin Core: @qbrainai/qbrain-ai-agent-core and the elements it uses or is used by." {
    title "@qbrainai/qbrain-ai-agent-core: neighbours (1 of 1)"
    include qbrain.plugins.agentCorePackage qbrain.repoFs
    exclude "element.tag==Planned"
    exclude "relationship.tag==Planned"
    autolayout tb 100 50
}
component qbrain.libs "CMP-LIBS-CLIENT-01" "C4 level 3, Shared Libraries: QBrainAi.Client and the elements it uses or is used by." {
    title "QBrainAi.Client: neighbours (1 of 3)"
    include qbrain.libs.client qbrain.repl qbrain.libs.mcpAgent
    exclude "element.tag==Planned"
    exclude "relationship.tag==Planned"
    autolayout tb 100 50
}
component qbrain.libs "CMP-LIBS-CLIENT-02" "C4 level 3, Shared Libraries: QBrainAi.Client and the elements it uses or is used by." {
    title "QBrainAi.Client: neighbours (2 of 3)"
    include qbrain.libs.client qbrain.libs.replCore qbrain.compat
    exclude "element.tag==Planned"
    exclude "relationship.tag==Planned"
    autolayout tb 100 50
}
component qbrain.libs "CMP-LIBS-CLIENT-03" "C4 level 3, Shared Libraries: QBrainAi.Client and the elements it uses or is used by." {
    title "QBrainAi.Client: neighbours (3 of 3)"
    include qbrain.libs.client qbrain.testSuites qbrain.server
    exclude "element.tag==Planned"
    exclude "relationship.tag==Planned"
    autolayout tb 100 50
}
component qbrain.libs "CMP-LIBS-MCPAGENT-01" "C4 level 3, Shared Libraries: QBrainAi.McpAgent and the elements it uses or is used by." {
    title "QBrainAi.McpAgent: neighbours (1 of 2)"
    include qbrain.libs.mcpAgent qbrain.libs.replCore qbrain.compat qbrain.sampleHost
    exclude "element.tag==Planned"
    exclude "relationship.tag==Planned"
    autolayout tb 100 50
}
component qbrain.libs "CMP-LIBS-MCPAGENT-02" "C4 level 3, Shared Libraries: QBrainAi.McpAgent and the elements it uses or is used by." {
    title "QBrainAi.McpAgent: neighbours (2 of 2)"
    include qbrain.libs.mcpAgent qbrain.testSuites qbrain.qbagent qbrain.server
    exclude "element.tag==Planned"
    exclude "relationship.tag==Planned"
    autolayout tb 100 50
}
component qbrain.libs "CMP-LIBS-REPLCORE-01" "C4 level 3, Shared Libraries: QBrainAi.Repl.Core and the elements it uses or is used by." {
    title "QBrainAi.Repl.Core: neighbours (1 of 2)"
    include qbrain.libs.replCore qbrain.libs.agentCli qbrain.repl
    exclude "element.tag==Planned"
    exclude "relationship.tag==Planned"
    autolayout tb 100 50
}
component qbrain.libs "CMP-LIBS-REPLCORE-02" "C4 level 3, Shared Libraries: QBrainAi.Repl.Core and the elements it uses or is used by." {
    title "QBrainAi.Repl.Core: neighbours (2 of 2)"
    include qbrain.libs.replCore qbrain.testSuites
    exclude "element.tag==Planned"
    exclude "relationship.tag==Planned"
    autolayout tb 100 50
}
component qbrain.libs "CMP-LIBS-AGENTCLI-01" "C4 level 3, Shared Libraries: QBrainAi.Common.AgentCli and the elements it uses or is used by." {
    title "QBrainAi.Common.AgentCli: neighbours (1 of 1)"
    include qbrain.libs.agentCli qbrain.server qbrain.qbagent
    exclude "element.tag==Planned"
    exclude "relationship.tag==Planned"
    autolayout tb 100 50
}
component qbrain.libs "CMP-LIBS-SERVICEDEFAULTS-01" "C4 level 3, Shared Libraries: QBrainAi.ServiceDefaults and the elements it uses or is used by." {
    title "QBrainAi.ServiceDefaults: neighbours (1 of 2)"
    include qbrain.libs.serviceDefaults qbrain.server qbrain.keyserver
    exclude "element.tag==Planned"
    exclude "relationship.tag==Planned"
    autolayout tb 100 50
}
component qbrain.libs "CMP-LIBS-SERVICEDEFAULTS-02" "C4 level 3, Shared Libraries: QBrainAi.ServiceDefaults and the elements it uses or is used by." {
    title "QBrainAi.ServiceDefaults: neighbours (2 of 2)"
    include qbrain.libs.serviceDefaults qbrain.subscriber
    exclude "element.tag==Planned"
    exclude "relationship.tag==Planned"
    autolayout tb 100 50
}
component qbrain.libs "CMP-LIBS-TRANSACTIONSECURITY-01" "C4 level 3, Shared Libraries: QBrainAi.TransactionSecurity and the elements it uses or is used by." {
    title "QBrainAi.TransactionSecurity: neighbours (1 of 3)"
    include qbrain.libs.transactionSecurity qbrain.server qbrain.keyserver
    exclude "element.tag==Planned"
    exclude "relationship.tag==Planned"
    autolayout tb 100 50
}
component qbrain.libs "CMP-LIBS-TRANSACTIONSECURITY-02" "C4 level 3, Shared Libraries: QBrainAi.TransactionSecurity and the elements it uses or is used by." {
    title "QBrainAi.TransactionSecurity: neighbours (2 of 3)"
    include qbrain.libs.transactionSecurity qbrain.subscriber qbrain.repl
    exclude "element.tag==Planned"
    exclude "relationship.tag==Planned"
    autolayout tb 100 50
}
component qbrain.libs "CMP-LIBS-TRANSACTIONSECURITY-03" "C4 level 3, Shared Libraries: QBrainAi.TransactionSecurity and the elements it uses or is used by." {
    title "QBrainAi.TransactionSecurity: neighbours (3 of 3)"
    include qbrain.libs.transactionSecurity qbrain.testSuites
    exclude "element.tag==Planned"
    exclude "relationship.tag==Planned"
    autolayout tb 100 50
}
component qbrain.compat "CMP-COMPAT-COMPATCLIENT-01" "C4 level 3, Compatibility Packages: SharpNinja.McpServer.Client and the elements it uses or is used by." {
    title "SharpNinja.McpServer.Client: neighbours (1 of 1)"
    include qbrain.compat.compatClient qbrain.libs
    exclude "element.tag==Planned"
    exclude "relationship.tag==Planned"
    autolayout tb 100 50
}
component qbrain.compat "CMP-COMPAT-COMPATCQRS-01" "C4 level 3, Compatibility Packages: SharpNinja.McpServer.Cqrs and the elements it uses or is used by." {
    title "SharpNinja.McpServer.Cqrs: neighbours (1 of 1)"
    include qbrain.compat.compatCqrs qbrain.server
    exclude "element.tag==Planned"
    exclude "relationship.tag==Planned"
    autolayout tb 100 50
}
component qbrain.compat "CMP-COMPAT-COMPATCQRSMVVM-01" "C4 level 3, Compatibility Packages: SharpNinja.McpServer.Cqrs.Mvvm and the elements it uses or is used by." {
    title "SharpNinja.McpServer.Cqrs.Mvvm: neighbours (1 of 1)"
    include qbrain.compat.compatCqrsMvvm qbrain.server
    exclude "element.tag==Planned"
    exclude "relationship.tag==Planned"
    autolayout tb 100 50
}
component qbrain.compat "CMP-COMPAT-COMPATMCPAGENT-01" "C4 level 3, Compatibility Packages: SharpNinja.McpServer.McpAgent and the elements it uses or is used by." {
    title "SharpNinja.McpServer.McpAgent: neighbours (1 of 1)"
    include qbrain.compat.compatMcpAgent qbrain.libs
    exclude "element.tag==Planned"
    exclude "relationship.tag==Planned"
    autolayout tb 100 50
}
component qbrain.compat "CMP-COMPAT-COMPATREPLCORE-01" "C4 level 3, Compatibility Packages: SharpNinja.McpServer.Repl.Core and the elements it uses or is used by." {
    title "SharpNinja.McpServer.Repl.Core: neighbours (1 of 1)"
    include qbrain.compat.compatReplCore qbrain.libs
    exclude "element.tag==Planned"
    exclude "relationship.tag==Planned"
    autolayout tb 100 50
}
component qbrain.compat "CMP-COMPAT-COMPATREPL-01" "C4 level 3, Compatibility Packages: SharpNinja.McpServer.Repl and the elements it uses or is used by." {
    title "SharpNinja.McpServer.Repl: neighbours (1 of 1)"
    include qbrain.compat.compatRepl qbrain.repl
    exclude "element.tag==Planned"
    exclude "relationship.tag==Planned"
    autolayout tb 100 50
}
component qbrain.compat "CMP-COMPAT-COMPATQBAGENT-01" "C4 level 3, Compatibility Packages: SharpNinja.McpServer.QBAgent and the elements it uses or is used by." {
    title "SharpNinja.McpServer.QBAgent: neighbours (1 of 1)"
    include qbrain.compat.compatQBAgent qbrain.qbagent
    exclude "element.tag==Planned"
    exclude "relationship.tag==Planned"
    autolayout tb 100 50
}
component qbrain.testSuites "CMP-TESTSUITES-TESTSUPPORTMCP-01" "C4 level 3, Test Suites: QBrainAi.Support.Mcp.Tests and the elements it uses or is used by." {
    title "QBrainAi.Support.Mcp.Tests: neighbours (1 of 1)"
    include qbrain.testSuites.testSupportMcp qbrain.server qbrain.testSuites.testProcessHelper
    exclude "element.tag==Planned"
    exclude "relationship.tag==Planned"
    autolayout tb 100 50
}
component qbrain.testSuites "CMP-TESTSUITES-TESTSUPPORTMCPINTEGRATION-01" "C4 level 3, Test Suites: QBrainAi.Support.Mcp.IntegrationTests and the elements it uses or is used by." {
    title "QBrainAi.Support.Mcp.IntegrationTests: neighbours (1 of 1)"
    include qbrain.testSuites.testSupportMcpIntegration qbrain.server qbrain.repl qbrain.qbagent
    exclude "element.tag==Planned"
    exclude "relationship.tag==Planned"
    autolayout tb 100 50
}
component qbrain.testSuites "CMP-TESTSUITES-TESTCLIENT-01" "C4 level 3, Test Suites: QBrainAi.Client.Tests and the elements it uses or is used by." {
    title "QBrainAi.Client.Tests: neighbours (1 of 1)"
    include qbrain.testSuites.testClient qbrain.libs
    exclude "element.tag==Planned"
    exclude "relationship.tag==Planned"
    autolayout tb 100 50
}
component qbrain.testSuites "CMP-TESTSUITES-TESTCQRS-01" "C4 level 3, Test Suites: QBrainAi.Cqrs.Tests and the elements it uses or is used by." {
    title "QBrainAi.Cqrs.Tests: neighbours (1 of 1)"
    include qbrain.testSuites.testCqrs qbrain.server
    exclude "element.tag==Planned"
    exclude "relationship.tag==Planned"
    autolayout tb 100 50
}
component qbrain.testSuites "CMP-TESTSUITES-TESTLAUNCHER-01" "C4 level 3, Test Suites: QBrainAi.Launcher.Tests and the elements it uses or is used by." {
    title "QBrainAi.Launcher.Tests: neighbours (1 of 1)"
    include qbrain.testSuites.testLauncher qbrain.launcher
    exclude "element.tag==Planned"
    exclude "relationship.tag==Planned"
    autolayout tb 100 50
}
component qbrain.testSuites "CMP-TESTSUITES-TESTMCPAGENT-01" "C4 level 3, Test Suites: QBrainAi.McpAgent.Tests and the elements it uses or is used by." {
    title "QBrainAi.McpAgent.Tests: neighbours (1 of 1)"
    include qbrain.testSuites.testMcpAgent qbrain.libs
    exclude "element.tag==Planned"
    exclude "relationship.tag==Planned"
    autolayout tb 100 50
}
component qbrain.testSuites "CMP-TESTSUITES-TESTQBAGENT-01" "C4 level 3, Test Suites: QBrainAi.QBAgent.Tests and the elements it uses or is used by." {
    title "QBrainAi.QBAgent.Tests: neighbours (1 of 1)"
    include qbrain.testSuites.testQBAgent qbrain.qbagent
    exclude "element.tag==Planned"
    exclude "relationship.tag==Planned"
    autolayout tb 100 50
}
component qbrain.testSuites "CMP-TESTSUITES-TESTREPLCORE-01" "C4 level 3, Test Suites: QBrainAi.Repl.Core.Tests and the elements it uses or is used by." {
    title "QBrainAi.Repl.Core.Tests: neighbours (1 of 1)"
    include qbrain.testSuites.testReplCore qbrain.libs
    exclude "element.tag==Planned"
    exclude "relationship.tag==Planned"
    autolayout tb 100 50
}
component qbrain.testSuites "CMP-TESTSUITES-TESTREPLINTEGRATION-01" "C4 level 3, Test Suites: QBrainAi.Repl.IntegrationTests and the elements it uses or is used by." {
    title "QBrainAi.Repl.IntegrationTests: neighbours (1 of 1)"
    include qbrain.testSuites.testReplIntegration qbrain.repl
    exclude "element.tag==Planned"
    exclude "relationship.tag==Planned"
    autolayout tb 100 50
}
component qbrain.testSuites "CMP-TESTSUITES-TESTACID-01" "C4 level 3, Test Suites: QBrainAi.Acid.IntegrationTests and the elements it uses or is used by." {
    title "QBrainAi.Acid.IntegrationTests: neighbours (1 of 1)"
    include qbrain.testSuites.testAcid qbrain.keyserver qbrain.subscriber
    exclude "element.tag==Planned"
    exclude "relationship.tag==Planned"
    autolayout tb 100 50
}
component qbrain.testSuites "CMP-TESTSUITES-TESTTXSEC-01" "C4 level 3, Test Suites: QBrainAi.TransactionSecurity.IntegrationTests and the elements it uses or is used by." {
    title "QBrainAi.TransactionSecurity.IntegrationTests: neighbours (1 of 2)"
    include qbrain.testSuites.testTxSec qbrain.keyserver qbrain.subscriber
    exclude "element.tag==Planned"
    exclude "relationship.tag==Planned"
    autolayout tb 100 50
}
component qbrain.testSuites "CMP-TESTSUITES-TESTTXSEC-02" "C4 level 3, Test Suites: QBrainAi.TransactionSecurity.IntegrationTests and the elements it uses or is used by." {
    title "QBrainAi.TransactionSecurity.IntegrationTests: neighbours (2 of 2)"
    include qbrain.testSuites.testTxSec qbrain.libs
    exclude "element.tag==Planned"
    exclude "relationship.tag==Planned"
    autolayout tb 100 50
}
component qbrain.testSuites "CMP-TESTSUITES-TESTPLUGININTEGRATION-01" "C4 level 3, Test Suites: QBrainAi.PluginIntegration.Tests and the elements it uses or is used by." {
    title "QBrainAi.PluginIntegration.Tests: neighbours (1 of 2)"
    include qbrain.testSuites.testPluginIntegration llm qbrain.server
    exclude "element.tag==Planned"
    exclude "relationship.tag==Planned"
    autolayout tb 100 50
}
component qbrain.testSuites "CMP-TESTSUITES-TESTPLUGININTEGRATION-02" "C4 level 3, Test Suites: QBrainAi.PluginIntegration.Tests and the elements it uses or is used by." {
    title "QBrainAi.PluginIntegration.Tests: neighbours (2 of 2)"
    include qbrain.testSuites.testPluginIntegration qbrain.nukeBuild
    exclude "element.tag==Planned"
    exclude "relationship.tag==Planned"
    autolayout tb 100 50
}
component qbrain.testSuites "CMP-TESTSUITES-TESTPLANREVIEW-01" "C4 level 3, Test Suites: QBrainAi.PlanReview.Tests and the elements it uses or is used by." {
    title "QBrainAi.PlanReview.Tests: neighbours (1 of 1)"
    include qbrain.testSuites.testPlanReview llm
    exclude "element.tag==Planned"
    exclude "relationship.tag==Planned"
    autolayout tb 100 50
}
component qbrain.testSuites "CMP-TESTSUITES-TESTREVIEW-01" "C4 level 3, Test Suites: QBrainAi.Review.Tests and the elements it uses or is used by." {
    title "QBrainAi.Review.Tests: neighbours (1 of 1)"
    include qbrain.testSuites.testReview llm qbrain.nukeBuild
    exclude "element.tag==Planned"
    exclude "relationship.tag==Planned"
    autolayout tb 100 50
}
component qbrain.testSuites "CMP-TESTSUITES-TESTBUILD-01" "C4 level 3, Test Suites: Build.Tests and the elements it uses or is used by." {
    title "Build.Tests: neighbours (1 of 1)"
    include qbrain.testSuites.testBuild qbrain.nukeBuild
    exclude "element.tag==Planned"
    exclude "relationship.tag==Planned"
    autolayout tb 100 50
}
component qbrain.testSuites "CMP-TESTSUITES-VALIDATIONCONTEXT-01" "C4 level 3, Test Suites: QBrainAi.Context.Validation and the elements it uses or is used by." {
    title "QBrainAi.Context.Validation: neighbours (1 of 1)"
    include qbrain.testSuites.validationContext qbrain.server
    exclude "element.tag==Planned"
    exclude "relationship.tag==Planned"
    autolayout tb 100 50
}
component qbrain.testSuites "CMP-TESTSUITES-VALIDATIONGITHUB-01" "C4 level 3, Test Suites: QBrainAi.GitHub.Validation and the elements it uses or is used by." {
    title "QBrainAi.GitHub.Validation: neighbours (1 of 1)"
    include qbrain.testSuites.validationGitHub qbrain.server
    exclude "element.tag==Planned"
    exclude "relationship.tag==Planned"
    autolayout tb 100 50
}
component qbrain.testSuites "CMP-TESTSUITES-VALIDATIONREPO-01" "C4 level 3, Test Suites: QBrainAi.Repo.Validation and the elements it uses or is used by." {
    title "QBrainAi.Repo.Validation: neighbours (1 of 1)"
    include qbrain.testSuites.validationRepo qbrain.server
    exclude "element.tag==Planned"
    exclude "relationship.tag==Planned"
    autolayout tb 100 50
}
component qbrain.testSuites "CMP-TESTSUITES-VALIDATIONSESSIONLOG-01" "C4 level 3, Test Suites: QBrainAi.SessionLog.Validation and the elements it uses or is used by." {
    title "QBrainAi.SessionLog.Validation: neighbours (1 of 1)"
    include qbrain.testSuites.validationSessionLog qbrain.server
    exclude "element.tag==Planned"
    exclude "relationship.tag==Planned"
    autolayout tb 100 50
}
component qbrain.testSuites "CMP-TESTSUITES-VALIDATIONTODO-01" "C4 level 3, Test Suites: QBrainAi.Todo.Validation and the elements it uses or is used by." {
    title "QBrainAi.Todo.Validation: neighbours (1 of 1)"
    include qbrain.testSuites.validationTodo qbrain.server
    exclude "element.tag==Planned"
    exclude "relationship.tag==Planned"
    autolayout tb 100 50
}
component qbrain.testSuites "CMP-TESTSUITES-VALIDATIONTOOLREGISTRY-01" "C4 level 3, Test Suites: QBrainAi.ToolRegistry.Validation and the elements it uses or is used by." {
    title "QBrainAi.ToolRegistry.Validation: neighbours (1 of 1)"
    include qbrain.testSuites.validationToolRegistry qbrain.server
    exclude "element.tag==Planned"
    exclude "relationship.tag==Planned"
    autolayout tb 100 50
}
component qbrain.testSuites "CMP-TESTSUITES-VALIDATIONWORKSPACE-01" "C4 level 3, Test Suites: QBrainAi.Workspace.Validation and the elements it uses or is used by." {
    title "QBrainAi.Workspace.Validation: neighbours (1 of 1)"
    include qbrain.testSuites.validationWorkspace qbrain.server
    exclude "element.tag==Planned"
    exclude "relationship.tag==Planned"
    autolayout tb 100 50
}
component qbrain.devTools "CMP-DEVTOOLS-PSCONTEXT-01" "C4 level 3, Developer Tools: McpContext, McpSession, McpTodo modules and the elements it uses or is used by." {
    title "McpContext, McpSession, McpTodo modules: neighbours (1 of 1)"
    include qbrain.devTools.psContext qbrain.server
    exclude "element.tag==Planned"
    exclude "relationship.tag==Planned"
    autolayout tb 100 50
}
component qbrain.devTools "CMP-DEVTOOLS-PSREPL-01" "C4 level 3, Developer Tools: McpRepl module and the elements it uses or is used by." {
    title "McpRepl module: neighbours (1 of 1)"
    include qbrain.devTools.psRepl qbrain.repl qbrain.plugins
    exclude "element.tag==Planned"
    exclude "relationship.tag==Planned"
    autolayout tb 100 50
}
component qbrain.devTools "CMP-DEVTOOLS-TSREPL-01" "C4 level 3, Developer Tools: @qbrainai/qbrain-ai-repl and the elements it uses or is used by." {
    title "@qbrainai/qbrain-ai-repl: neighbours (1 of 1)"
    include qbrain.devTools.tsRepl qbrain.repl qbrain.plugins
    exclude "element.tag==Planned"
    exclude "relationship.tag==Planned"
    autolayout tb 100 50
}
component qbrain.devTools "CMP-DEVTOOLS-BASHHELPERS-01" "C4 level 3, Developer Tools: Bash session and TODO helpers and the elements it uses or is used by." {
    title "Bash session and TODO helpers: neighbours (1 of 1)"
    include qbrain.devTools.bashHelpers qbrain.server
    exclude "element.tag==Planned"
    exclude "relationship.tag==Planned"
    autolayout tb 100 50
}
component qbrain.devTools "CMP-DEVTOOLS-MEMORYBENCH-01" "C4 level 3, Developer Tools: MemoryBenchLiveSubscription and the elements it uses or is used by." {
    title "MemoryBenchLiveSubscription: neighbours (1 of 1)"
    include qbrain.devTools.memoryBench qbrain.server
    exclude "element.tag==Planned"
    exclude "relationship.tag==Planned"
    autolayout tb 100 50
}
component qbrain.devTools "CMP-DEVTOOLS-UNITGATE-01" "C4 level 3, Developer Tools: Invoke-SessionLifeUnitGate and the elements it uses or is used by." {
    title "Invoke-SessionLifeUnitGate: neighbours (1 of 1)"
    include qbrain.devTools.unitGate qbrain.nukeBuild
    exclude "element.tag==Planned"
    exclude "relationship.tag==Planned"
    autolayout tb 100 50
}
component qbrain.devTools "CMP-DEVTOOLS-EVIDENCESCRIPTS-01" "C4 level 3, Developer Tools: Evidence collector scripts and the elements it uses or is used by." {
    title "Evidence collector scripts: neighbours (1 of 1)"
    include qbrain.devTools.evidenceScripts qbrain.server
    exclude "element.tag==Planned"
    exclude "relationship.tag==Planned"
    autolayout tb 100 50
}
component qbrain.opsScripts "CMP-OPSSCRIPTS-OPSSERVICE-01" "C4 level 3, Operations Scripts: Service management and the elements it uses or is used by." {
    title "Service management: neighbours (1 of 1)"
    include qbrain.opsScripts.opsService qbrain.server
    exclude "element.tag==Planned"
    exclude "relationship.tag==Planned"
    autolayout tb 100 50
}
component qbrain.opsScripts "CMP-OPSSCRIPTS-OPSIDENTITY-01" "C4 level 3, Operations Scripts: Identity setup and the elements it uses or is used by." {
    title "Identity setup: neighbours (1 of 1)"
    include qbrain.opsScripts.opsIdentity oidc
    exclude "element.tag==Planned"
    exclude "relationship.tag==Planned"
    autolayout tb 100 50
}
component qbrain.opsScripts "CMP-OPSSCRIPTS-OPSPACKAGING-01" "C4 level 3, Operations Scripts: Packaging and the elements it uses or is used by." {
    title "Packaging: neighbours (1 of 1)"
    include qbrain.opsScripts.opsPackaging qbrain.repl
    exclude "element.tag==Planned"
    exclude "relationship.tag==Planned"
    autolayout tb 100 50
}
component qbrain.opsScripts "CMP-OPSSCRIPTS-OPSDATA-01" "C4 level 3, Operations Scripts: Data maintenance and the elements it uses or is used by." {
    title "Data maintenance: neighbours (1 of 1)"
    include qbrain.opsScripts.opsData qbrain.server
    exclude "element.tag==Planned"
    exclude "relationship.tag==Planned"
    autolayout tb 100 50
}
component qbrain.opsScripts "CMP-OPSSCRIPTS-OPSVALIDATION-01" "C4 level 3, Operations Scripts: Validation and smoke tests and the elements it uses or is used by." {
    title "Validation and smoke tests: neighbours (1 of 1)"
    include qbrain.opsScripts.opsValidation qbrain.server
    exclude "element.tag==Planned"
    exclude "relationship.tag==Planned"
    autolayout tb 100 50
}
component qbrain.opsScripts "CMP-OPSSCRIPTS-OPSREBRAND-01" "C4 level 3, Operations Scripts: Rebrand scripts and the elements it uses or is used by." {
    title "Rebrand scripts: neighbours (1 of 1)"
    include qbrain.opsScripts.opsRebrand qbrain.repoFs
    exclude "element.tag==Planned"
    exclude "relationship.tag==Planned"
    autolayout tb 100 50
}
component qbrain.qbagent "CMP-QBAGENT-RUNLOOP-01" "C4 level 3, QBAgent: QBAgent Run Loop and the elements it uses or is used by." {
    title "QBAgent Run Loop: neighbours (1 of 2)"
    include qbrain.qbagent.runLoop llm qbrain.libs
    exclude "element.tag==Planned"
    exclude "relationship.tag==Planned"
    autolayout tb 100 50
}
component qbrain.qbagent "CMP-QBAGENT-RUNLOOP-02" "C4 level 3, QBAgent: QBAgent Run Loop and the elements it uses or is used by." {
    title "QBAgent Run Loop: neighbours (2 of 2)"
    include qbrain.qbagent.runLoop qbrain.qbagent.agentTools
    exclude "element.tag==Planned"
    exclude "relationship.tag==Planned"
    autolayout tb 100 50
}
component qbrain.qbagent "CMP-QBAGENT-AGENTTOOLS-01" "C4 level 3, QBAgent: QBrainAi.QBAgent.Tools and the elements it uses or is used by." {
    title "QBrainAi.QBAgent.Tools: neighbours (1 of 1)"
    include qbrain.qbagent.agentTools qbrain.libs qbrain.qbagent.agentSkills
    exclude "element.tag==Planned"
    exclude "relationship.tag==Planned"
    autolayout tb 100 50
}
