workspace "QBrain.AI (McpServer)" "The one C4 definition of the whole QBrain.AI codebase and of every documentation diagram (PLAN-C4MODEL-001). Code evidence from commit 36783b79d061303bfb3e17abe685640a953491af and the P3 inventory of 2026-10-19; see evidence.md." {

    !identifiers hierarchical
    # One implied relationship per element pair at higher levels, so neighbourhood views stay readable (AC-5).
    !impliedRelationships "com.structurizr.model.CreateImpliedRelationshipsUnlessAnyRelationshipExistsStrategy"

    model {
        properties {
            "source-repo" "github.com/sharpninja/McpServer"
            "source-commit" "36783b79d061303bfb3e17abe685640a953491af"
        }

        developer = person "Developer / Operator" "Human who pairs agents with workspaces, manages TODOs, requirements and reviews through Swagger, /pair and the REPL."
        agent = person "AI Coding Agent" "Codex, Claude Code, Grok, Copilot, Cline, OpenCode and similar agents that read the marker file and call QBrain.AI for context, TODOs and session logs."

        github = softwareSystem "GitHub" "Issues, pull requests and repositories reached through the gh CLI." "External"
        llm = softwareSystem "LLM Providers and Agent CLIs" "grok-cli, codex-cli, copilot CLI and OpenAI-compatible endpoints used as brain slots and hosted agents." "External"
        tunnels = softwareSystem "Tunnel Providers" "Cloudflare tunnel, ngrok and a self-hosted FRP server that expose the local server port." "External"
        parseable = softwareSystem "Parseable" "Optional log ingestion service for Serilog events." "External"
        hf = softwareSystem "Hugging Face" "Hosts the all-MiniLM-L6-v2 ONNX model and vocab, auto-downloaded for embeddings." "External"
        oidc = softwareSystem "External OIDC Provider" "Optional external identity provider used instead of the embedded Duende IdentityServer." "External"
        peer = softwareSystem "Federated QBrain.AI Instance" "Upstream or peer QBrain.AI server used for federation of TODOs, session logs and GraphRAG." "External"
        pluginRepos = softwareSystem "Agent Plugin Repositories" "mcpserver-*-plugin repositories (claude-code, codex, copilot, grok, cline, opencode) that receive the synced plugin core; outside this repository." "External"

        qbrain = softwareSystem "QBrain.AI" "Workspace-scoped context, memory, TODO, requirements, session-log, triage and agent-orchestration platform for AI coding agents (formerly McpServer)." {
            !docs documentation
            !adrs decisions

            server = container "MCP Context Server" "Single ASP.NET Core host exposing REST, MCP HTTP transport, pairing UI, identity and background workers." "ASP.NET Core, .NET 10 (QBrainAi.Support.Mcp)" "Server" {
                api = component "REST Controllers" "About 36 controllers: todo, session log, requirements, use cases, memory, context, GraphRAG, handoff, hostile review, triage, agent pool, workspace, federation, tunnel, GitHub, repo, voice, products." "ASP.NET Core MVC (Controllers/*)"
                mcpHttp = component "MCP HTTP Transport" "MCP server endpoint at /mcp-transport." "ModelContextProtocol.AspNetCore (MapMcp)"
                stdioHost = component "MCP STDIO Host" "Same executable started with --transport stdio; runs MCP over stdin/stdout with its own service registrations (not a separate container)." "McpStdioHost"
                mcpTools = component "MCP Tool Surface" "FwhMcpTools.* and QBrainAiMcpTools: tool wrappers over context, todo, session log, requirements, memory, handoff, hostile review, triage, use cases, workspace validation." "MCP tools (McpStdio/*), shared by HTTP and STDIO transports"
                pairing = component "Pairing and API Key" "/pair, /pair/key, /pair/qr, /api-key issue workspace tokens and QR pairing." "Minimal APIs, WorkspaceTokenService, PairingSessionService"
                middleware = component "Request Pipeline" "Workspace resolution, workspace auth, federation and interaction logging middleware, JWT bearer auth, health checks, Swagger." "ASP.NET Core middleware"
                identity = component "Identity" "Embedded Duende IdentityServer with ASP.NET Identity, device flow, or external OIDC via strategy." "Duende IdentityServer, IOidcProviderStrategy"
                workspace = component "Workspace Service" "Resolves workspaces and marker files, readiness, process manager, policy, projection writer." "QBrainAi.Services"
                todo = component "TODO Service" "EF-backed TODOs, creation, update, validation, execution, YAML bootstrap import, GitHub issue sync." "QBrainAi.Services (EfTodoService, TodoExecutionService)"
                sessionLog = component "Session Log Service" "Sessions, turns, actions, dialogs, sanitizing, turn-context validation, file watcher import, transcript ingestion." "QBrainAi.Services, QBrainAi.SessionLog.Transcripts"
                requirements = component "Requirements and Use Cases" "FR/TR/TEST documents, traceability, use case CQRS, FR coverage gate, recovery runs." "QBrainAi.Services (Requirements, UseCases)"
                memory = component "Memory Service" "Layered memory with consolidate, promote, explore, competitive ops, session-log hook, bench harness." "QBrainAi.Services (Memory)"
                search = component "Context Search and Ingestion" "FTS5, vector and hybrid search; ingestors for repo, GitHub issues, external docs, websites and session logs; embeddings and chunker." "QBrainAi.Services, QBrainAi.Storage (Indexing)"
                graphrag = component "GraphRAG" "CQRS commands and queries, internal fallback or external-command backend, federated GraphRAG, global corpus seeder." ".NET class library (src/QBrainAi.GraphRag)" "Library"
                triage = component "Triage, Handoff and Hostile Review" "Triage queue worker and research runner, handoff ingestion, hostile review director and service." "QBrainAi.Services"
                brain = component "Brain Slots and Quad Brain" "Brain slot registry, credential resolution, CLI and OpenAI chat clients, quad-brain orchestration and OpenAI-compatible controller." "Support.Mcp/Services, config/brain-slots"
                agents = component "Agent Pool and Processes" "Agent pool, health monitor, process manager, execution strategies (codex, copilot, grok, hosted, one-shot), branch and isolation strategies." "QBrainAi.Services, QBrainAi.McpAgent hosting"
                federation = component "Federation" "Registry, topology, envelope signing, proxy, queued replay, fanout sync, conflict and outbox." "QBrainAi.Services (Federation*)"
                tunnel = component "Tunnel Registry" "Starts and tracks Cloudflare, ngrok or FRP tunnels." "QBrainAi.Services (TunnelRegistry, *TunnelProvider)"
                github = component "GitHub Integration" "gh CLI wrapper, workspace token store, issue-todo sync." "GitHubCliService"
                txsec = component "Transaction Security" "Turn transaction coordinator, diffgram protector, pub/sub replay worker, gated service decorators (TransactionGated*)." "QBrainAi.TransactionSecurity"
                tools = component "Tool Registry, Templates and Products" "Tool registry and buckets, prompt templates, products and workspace sharing." "QBrainAi.Services, Support.Mcp/Products"
                storage = component "Storage and Providers" "EF Core DbContext, provider strategies for SQLite, PostgreSQL, SQL Server, migration and encryption coordinators, startup bootstrap." "QBrainAi.Storage and *Migrations"
                mcpDbContext = component "McpDbContext" "Main EF Core context: 71 entity types (workspaces, session logs, tools and agents, TODOs, requirements and use cases, memory and graph, triage, federation, handoff, hostile review). Workspace foreign keys, soft-delete filters and workspace query filters. Detail in data/ef-entity-model.md." "EF Core DbContext (QBrainAi.Storage/McpDbContext.cs)"
                identityDbContext = component "McpIdentityDbContext" "ASP.NET Core Identity context: 7 framework entity types (McpUser, IdentityRole, user and role claims, logins, tokens, user roles). SQL Server only, schema by EnsureCreated, no migrations." "IdentityDbContext<McpUser> (Support.Mcp/Identity)"
                txDbContext = component "TransactionSecurityDbContext" "Internal context for transaction security state: 10 entity types (key server parties, keys, sequences, nonces, manifests, subscriber transactions, audit events, pub/sub messages). SQLite only, EnsureCreated, no migrations." "EF Core DbContext (QBrainAi.TransactionSecurity)"
                sqliteMigrations = component "QBrainAi.Storage.SqliteMigrations" "SQLite migrations and model snapshot of McpDbContext (41 at evidence time), applied by McpDatabaseMigrationCoordinator (MigrateAsync)." ".NET class library, EF Core migrations" "Library"
                sqlServerMigrations = component "QBrainAi.Storage.SqlServerMigrations" "SQL Server migrations and model snapshot of McpDbContext (41 at evidence time), applied by McpDatabaseMigrationCoordinator (MigrateAsync)." ".NET class library, EF Core migrations" "Library"
                postgresMigrations = component "QBrainAi.Storage.PostgreSqlMigrations" "PostgreSQL migrations and model snapshot of McpDbContext (41 at evidence time), applied by McpDatabaseMigrationCoordinator (MigrateAsync)." ".NET class library, EF Core migrations" "Library"
                servicesLib = component "QBrainAi.Services" "Application services of the server: TODO, session log, requirements, memory, search and ingestion, triage, workspace, agents, federation, tunnels, GitHub, templates and products." ".NET class library (src/QBrainAi.Services)" "Library"
                storageLib = component "QBrainAi.Storage" "EF Core McpDbContext, provider strategies, FTS5 and HNSW indexing, ONNX embeddings, migration and encryption coordinators; also holds the legacy pre-split migrations." ".NET class library (src/QBrainAi.Storage)" "Library"
                transcriptsLib = component "QBrainAi.SessionLog.Transcripts" "Agent transcript adapters (Claude, Codex and others) feeding session-log ingestion." ".NET class library (src/QBrainAi.SessionLog.Transcripts)" "Library"
                cqrsLib = component "QBrainAi.Cqrs" "Async CQRS framework with Result monad, correlation ids and pipeline behaviors (package QBrainAI.Cqrs)." ".NET class library (src/QBrainAi.Cqrs)" "Library"
                cqrsMvvmLib = component "QBrainAi.Cqrs.Mvvm" "MVVM bridge dispatching CQRS commands and queries through relay commands (package QBrainAI.Cqrs.Mvvm)." ".NET class library (src/QBrainAi.Cqrs.Mvvm)" "Library"
            }

            txDb = container "Transaction State Database" "Optional SQLite file for transaction security state (pub/sub broker, sequences, nonces, manifests, audit) when a database path is configured; otherwise state is in memory." "SQLite file" "Database"
            db = container "MCP Database" "Relational store for TODOs, session logs, requirements, memory, graph, federation, identity and audit data. Default file mcp.db in /data." "SQLite (default), PostgreSQL or SQL Server via EF Core" "Database"
            vector = container "Vector Index" "HNSW index file used for semantic search, at /data/vector.idx." "HnswIndex file, ONNX embeddings" "Database"
            repoFs = container "Workspace Files" "Mounted repository: AGENTS-README-FIRST marker, docs/Project/TODO.yaml, docs/sessions, requirements docs." "Filesystem (/workspace)" "Database"
            keyserver = container "Key Server" "Optional standalone host of the in-memory key server (InMemoryKeyServerService): registers parties, serves public keys, signs and verifies transaction manifests. The MCP Context Server and REPL embed the same services in-process by default." "ASP.NET Core minimal API (QBrainAi.KeyServer)" "Server" {
                ksApi = component "Key Server Endpoints" "POST /qbrainai/keyserver/parties, GET parties/{id}/keys/{id}, POST manifests/sign and verify, GET manifests/report." "Minimal APIs"
                ksRegistry = component "Party Registry and Manifest Service" "IKeyServerPartyRegistry, IKeyServerManifestService, key provisioning." "QBrainAi.TransactionSecurity"
            }
            subscriber = container "Transaction Subscriber" "Optional standalone subscriber host (AddHttpTransactionSubscriber): receives diffgram commits over HTTP and verifies manifests through the Key Server. Default server configuration uses an in-process subscriber instead." "ASP.NET Core minimal API (QBrainAi.Subscriber)" "Server"
            repl = container "REPL Host" "Per-agent YAML-over-STDIO and interactive REPL that proxies workflows (todo, session log, memory, requirements, GraphRAG, handoff, triage) to the server REST API. dotnet tool qbrain-ai-repl (package QBrainAI.Repl)." "Console app, .NET 10, System.CommandLine (QBrainAi.Repl.Host)" "CLI" {
                stdio = component "Agent STDIO Handler" "Reads YAML envelopes on STDIN, writes responses; modes --agent-stdio and --interactive." "AgentStdioHandler, AgentStdioProtocol"
                dispatcher = component "REPL Command Dispatcher" "Routes commands to workflows, validates YAML messages, mutation policy." "QBrainAi.Repl.Core"
                workflows = component "Workflows" "Todo, SessionLog, Memory, Requirements, GraphRag, Handoff, HostileReview, Triage, WorkspaceValidation, AgentHelp, TranscriptIngestion workflows." "QBrainAi.Repl.Core"
                trust = component "Marker and Trust Bootstrap" "Finds AGENTS-README-FIRST marker, resolves workspace and client options, trust state, auth rotation." "MarkerFileClientOptionsResolver, TrustBootstrapService"
            }
            plugins = container "Agent Plugin Core" "Shared hook, marker-trust, cache and session-log libraries synced into each mcpserver-*-plugin repo for claude-code, codex, copilot, grok, cline, opencode hosts." "Bash, PowerShell, Node (plugins/core)" "CLI" {
                pluginPs = component "lib-ps" "PowerShell plugin core: marker trust, REPL invocation, session-log, TODO, triage and failsafe flows (plugins/core/lib-ps)." "PowerShell 7"
                pluginNode = component "lib-node" "Node plugin core: discovery, cache, runtime, tools and transport (plugins/core/lib-node)." "Node.js, TypeScript"
                pluginSh = component "lib-sh" "Bash plugin core (plugins/core/lib-sh)." "Bash"
                pluginHosts = component "Host adapters" "Per-host hook adapters with tests: claude-code, claude-cowork, cline, cline-v2, codex, copilot, grok, opencode (plugins/core/hosts)." "PowerShell, Node, Bash"
                pluginHooks = component "Hook templates and skills" "Hook templates and agent skills (handoff, hostile-review, memory, workspace-validation) shipped with every plugin (plugins/core/hooks-templates, plugins/core/skills)." "Templates, Markdown"
                pluginSync = component "Plugin sync" "Copies the core into each mcpserver-*-plugin repository (plugins/core/sync)." "PowerShell"
                agentCorePackage = component "@qbrainai/qbrain-ai-agent-core" "Shared TypeScript core for agent plugin parity: marker trust, enforcement state machine, cache manager (packages/mcpserver-agent-core)." "TypeScript npm package"
            }
            libs = container "Shared Libraries" "Class libraries loaded by two or more deployable units (PLAN-C4MODEL-001 D2, operator ruling 2026-10-19)." ".NET 10 class libraries" "Library" {
                client = component "QBrainAi.Client" "Typed REST client for every QBrain.AI endpoint: TODO, context, session log, repository, GitHub, workspace, tool registry, sync (package QBrainAI.Client)." ".NET class library (src/QBrainAi.Client)" "Library"
                mcpAgent = component "QBrainAi.McpAgent" "MCP agent integration on Microsoft Agent Framework with session-log, TODO, repository, desktop and local PowerShell workflows (package QBrainAI.McpAgent)." ".NET class library (src/QBrainAi.McpAgent)" "Library"
                replCore = component "QBrainAi.Repl.Core" "Reusable REPL workflows for session log, TODO, requirements and client passthrough (package QBrainAI.Repl.Core)." ".NET class library (src/QBrainAi.Repl.Core)" "Library"
                agentCli = component "QBrainAi.Common.AgentCli" "Shared agent CLI helpers used by the server, the REPL and the QBAgent tools." ".NET class library (src/QBrainAi.Common.AgentCli)" "Library"
                serviceDefaults = component "QBrainAi.ServiceDefaults" "OpenTelemetry, HTTP resilience and service discovery defaults for the web hosts." ".NET class library (src/QBrainAi.ServiceDefaults)" "Library"
                transactionSecurity = component "QBrainAi.TransactionSecurity" "Turn transaction coordinator, diffgram protection, key server and subscriber services, pub/sub broker and TransactionSecurityDbContext; in process by default (AddInProcessTransactionSecurity)." ".NET class library (src/QBrainAi.TransactionSecurity)" "Library"
            }
            compat = container "Compatibility Packages" "1.x package ids kept for existing consumers; public types moved to QBrainAi namespaces; removed at 2.0." ".NET 10 NuGet packages (src/Compatibility)" "Compatibility" {
                compatClient = component "SharpNinja.McpServer.Client" "Forwards to QBrainAI.Client." ".NET class library" "Compatibility"
                compatCqrs = component "SharpNinja.McpServer.Cqrs" "Forwards to QBrainAI.Cqrs." ".NET class library" "Compatibility"
                compatCqrsMvvm = component "SharpNinja.McpServer.Cqrs.Mvvm" "Forwards to QBrainAI.Cqrs.Mvvm." ".NET class library" "Compatibility"
                compatMcpAgent = component "SharpNinja.McpServer.McpAgent" "Forwards to QBrainAI.McpAgent." ".NET class library" "Compatibility"
                compatReplCore = component "SharpNinja.McpServer.Repl.Core" "Forwards to QBrainAI.Repl.Core." ".NET class library" "Compatibility"
                compatRepl = component "SharpNinja.McpServer.Repl" "1.x dotnet tool command mcpserver-repl; canonical is qbrain-ai-repl (QBrainAI.Repl)." "dotnet tool" "Compatibility"
                compatQBAgent = component "SharpNinja.McpServer.QBAgent" "1.x package id of the qbagent tool; canonical is QBrainAI.QBAgent." "dotnet tool" "Compatibility"
            }
            sampleHost = container "McpAgent Sample Host" "Console sample hosting QBrainAi.McpAgent with an OpenAI chat client and local PowerShell command execution." "Console app, .NET 10 (QBrainAi.McpAgent.SampleHost)" "CLI"
            testSuites = container "Test Suites" "xUnit v3 test projects (tests/); run through the Nuke Test and MigrationIntegrationTests targets; excluded from the runtime views." ".NET 10 test projects" "Test" {
                testSupportMcp = component "QBrainAi.Support.Mcp.Tests" "Server unit tests; Category=Integration tests run only through MigrationIntegrationTests." "xUnit v3" "Test"
                testSupportMcpIntegration = component "QBrainAi.Support.Mcp.IntegrationTests" "In-process server integration tests (CustomWebApplicationFactory) with REPL, QBAgent and SQLite migrations." "xUnit v3" "Test"
                testClient = component "QBrainAi.Client.Tests" "Client unit tests." "xUnit v3" "Test"
                testCqrs = component "QBrainAi.Cqrs.Tests" "CQRS and MVVM bridge unit tests." "xUnit v3" "Test"
                testLauncher = component "QBrainAi.Launcher.Tests" "Desktop launcher unit tests (Windows only)." "xUnit v3, net10.0-windows" "Test"
                testMcpAgent = component "QBrainAi.McpAgent.Tests" "McpAgent unit tests." "xUnit v3" "Test"
                testQBAgent = component "QBrainAi.QBAgent.Tests" "QBAgent and tool unit tests." "xUnit v3" "Test"
                testReplCore = component "QBrainAi.Repl.Core.Tests" "REPL workflow unit tests." "xUnit v3" "Test"
                testReplIntegration = component "QBrainAi.Repl.IntegrationTests" "REPL host integration tests." "xUnit v3" "Test"
                testAcid = component "QBrainAi.Acid.IntegrationTests" "ACID transaction tests across key server and subscriber hosts." "xUnit v3" "Test"
                testTxSec = component "QBrainAi.TransactionSecurity.IntegrationTests" "Transaction security integration tests across key server and subscriber hosts." "xUnit v3" "Test"
                testPluginIntegration = component "QBrainAi.PluginIntegration.Tests" "Plugin session-log integration tests (deterministic and AI)." "xUnit v3, aiUnit" "Test"
                testPlanReview = component "QBrainAi.PlanReview.Tests" "AI plan review tests." "xUnit v3, aiUnit" "Test"
                testReview = component "QBrainAi.Review.Tests" "AI code, project and warning-suppression review tests." "xUnit v3, aiUnit" "Test"
                testProcessHelper = component "QBrainAi.ProcessTree.TestHelper" "Helper executable for process-tree tests." "Console app" "Test"
                testBuild = component "Build.Tests" "Infrastructure tests of the Nuke build (run directly, not by the Test target)." "xUnit v3" "Test"
                validationContext = component "QBrainAi.Context.Validation" "Endpoint validation of the context API against a running server (port 7147)." "xUnit v3, HTTP" "Test"
                validationGitHub = component "QBrainAi.GitHub.Validation" "Endpoint validation of the GitHub API against a running server." "xUnit v3, HTTP" "Test"
                validationRepo = component "QBrainAi.Repo.Validation" "Endpoint validation of the repository API against a running server." "xUnit v3, HTTP" "Test"
                validationSessionLog = component "QBrainAi.SessionLog.Validation" "Endpoint validation of the session-log API against a running server." "xUnit v3, HTTP" "Test"
                validationTodo = component "QBrainAi.Todo.Validation" "Endpoint validation of the TODO API against a running server (TodoEndpointFixture, MCPSERVER_BASEURL)." "xUnit v3, HTTP" "Test"
                validationToolRegistry = component "QBrainAi.ToolRegistry.Validation" "Endpoint validation of the tool registry API against a running server." "xUnit v3, HTTP" "Test"
                validationWorkspace = component "QBrainAi.Workspace.Validation" "Endpoint validation of the workspace API against a running server." "xUnit v3, HTTP" "Test"
            }
            nukeBuild = container "Nuke Build" "Build orchestration: compile, test, migration integration tests, architecture diagrams, config and traceability validation, packaging, service update (build/_build.csproj, build.ps1)." "Nuke 9, .NET 10" "Build"
            devTools = container "Developer Tools" "Operator and agent tooling outside the product hosts (tools/)." "PowerShell, Bash, TypeScript, .NET" "Tooling" {
                psContext = component "McpContext, McpSession, McpTodo modules" "PowerShell modules for context ingestion and query, session logs and TODOs over the REST API; connection from AGENTS-README-FIRST.yaml." "PowerShell 7 modules"
                psRepl = component "McpRepl module" "Shared PowerShell module for agent plugins: typed REPL messages, YAML serialization, Invoke-McpRepl (published to the PowerShell Gallery)." "PowerShell 7 module"
                tsRepl = component "@qbrainai/qbrain-ai-repl" "Shared TypeScript surface for TS plugins: ReplBridge and McpAgentClient over qbrain-ai-repl --agent-stdio, marker resolution, cache (published to npm)." "TypeScript npm package"
                bashHelpers = component "Bash session and TODO helpers" "Bash functions for the session-log and TODO REST APIs (tools/bash)." "Bash"
                memoryBench = component "MemoryBenchLiveSubscription" "Memory benchmark runner writing docs/benchmarks/results; references QBrainAi.Services." "Console app, .NET 10"
                unitGate = component "Invoke-SessionLifeUnitGate" "Session-life cumulative unit gate: Pester, Nuke Test and Build.Tests with one run id (tools/validation)." "PowerShell 7"
                evidenceScripts = component "Evidence collector scripts" "Optional evidence collectors and persona launchers for reviews (Invoke-HostileValidator, Invoke-HostileGrok, Start-Astra, Start-Sol); not the hostile validator." "PowerShell 7"
            }
            opsScripts = container "Operations Scripts" "Service, identity, packaging, data and validation scripts (scripts/)." "PowerShell, Bash, Python" "Tooling" {
                opsService = component "Service management" "Update-McpService, Manage-McpService, Start-QBrainAi, Run-QBrainAi, Check-McpHealth." "PowerShell 7"
                opsIdentity = component "Identity setup" "Setup-McpKeycloak (PowerShell and Bash) and New-McpUser." "PowerShell 7, Bash"
                opsPackaging = component "Packaging" "Pack-ReplTool, Install-ReplTool, Package-QBrainAiMsix, Bump-GitVersionPatch, Set-AzureDevOpsNuGetSecret." "PowerShell 7"
                opsData = component "Data maintenance" "Migrate-McpTodoStorage, Manage-McpIndex, Invoke-McpDatabaseEncryptionTransition, Ingest-McpRequirements, Publish-RequirementsWiki, New-QuadBrainSlotConfiguration." "PowerShell 7"
                opsValidation = component "Validation and smoke tests" "Validate-McpConfig, Validate-RequirementsTraceability, Test-GraphRagSmoke, Test-McpMultiInstance, Test-McpNgrokTunnel." "PowerShell 7"
                opsRebrand = component "Rebrand scripts" "One-time QBrain.AI rebrand inventory and apply scripts (qbrain_ai_rebrand_*.py)." "Python"
            }
            qbagent = container "QBAgent" "Standalone interactive agent runtime with run loop, chat client, tools and skills, using McpAgent hosting and the client library. dotnet tool qbagent (package QBrainAI.QBAgent)." "Console app, .NET 10, Microsoft.Agents.AI (QBrainAi.QBAgent)" "CLI" {
                runLoop = component "QBAgent Run Loop" "Bootstrapper, run loop, progress and session-logging chat clients." "QBrainAi.QBAgent"
                agentTools = component "QBrainAi.QBAgent.Tools" "Bash, PowerShell, Git and file tools for the agent." ".NET class library (src/QBrainAi.QBAgent.Tools)" "Library"
                agentSkills = component "QBrainAi.QBAgent.Skills" "Skill catalog and skill tools for the agent." ".NET class library (src/QBrainAi.QBAgent.Skills)" "Library"
            }
            launcher = container "Desktop Launcher" "Windows helper that launches a process on the interactive desktop using CreateProcessWithTokenW; takes JSON argument and returns JSON on stdout." "Console exe, Win32 P/Invoke (QBrainAi.Launcher)" "CLI"
            frps = container "FRP Relay" "Self-hosted frps server (Railway or local compose) tunnelling the server port." "frp, Docker (infra/frp)" "Infrastructure"
        }


        # Context level
        developer -> qbrain "Manages workspaces, TODOs, requirements and reviews" "HTTPS (Swagger, /pair), REPL"
        agent -> qbrain "Reads context, updates TODOs, appends session logs" "MCP over STDIO or HTTP, REST, YAML STDIO REPL"
        qbrain -> github "Syncs issues, reads and writes PRs" "gh CLI"
        qbrain -> llm "Invokes brain slots and hosted agents" "CLI process or OpenAI-compatible HTTPS"
        qbrain -> tunnels "Exposes local port" "cloudflared, ngrok, frpc"
        qbrain -> parseable "Ships logs (optional)" "HTTPS JSON ingest"
        qbrain -> hf "Downloads embedding model" "HTTPS"
        qbrain -> oidc "Delegates authentication (optional)" "OIDC"
        qbrain -> peer "Federates TODOs, session logs, GraphRAG" "HTTPS, signed envelopes"

        # Container level
        developer -> qbrain.server "Uses Swagger UI and pairing pages" "HTTPS"
        developer -> qbrain.repl "Runs interactive REPL" "Terminal"
        agent -> qbrain.plugins "Triggers hooks" "Host hook scripts"
        agent -> qbrain.server "Calls MCP tools" "MCP HTTP /mcp-transport"
        agent -> qbrain.repl "Sends YAML commands" "STDIO"
        qbrain.plugins -> qbrain.repl "Starts and invokes via ensure-repl and repl-invoke" "Process, STDIO YAML"
        qbrain.repl -> qbrain.server "Workflow calls" "REST/JSON over HTTP with API key or JWT"
        qbrain.qbagent -> qbrain.server "Todo, session log, context calls" "REST/JSON over HTTP"
        qbrain.qbagent -> llm "Chat completions" "OpenAI-compatible HTTPS"
        qbrain.server -> qbrain.db "Reads and writes" "EF Core"
        qbrain.server -> qbrain.vector "Reads and writes index" "File I/O"
        qbrain.server -> qbrain.repoFs "Reads docs, writes projections and session logs" "File I/O"
        qbrain.server -> qbrain.subscriber "Optional: commits diffgrams over HTTP when PubSubTransport=Http (default is in-process)" "HTTP/JSON"
        qbrain.server -> qbrain.launcher "Spawns desktop processes" "Process args JSON, stdout JSON"
        qbrain.server -> github "Issues and PRs" "gh CLI"
        qbrain.server -> llm "Brain slots, agent execution" "CLI process, HTTPS"
        qbrain.server -> tunnels "Starts tunnel clients" "Process"
        qbrain.server -> qbrain.frps "Tunnel client connection" "frp"
        qbrain.server -> parseable "Logs" "HTTPS"
        qbrain.server -> hf "Model download" "HTTPS"
        qbrain.server -> oidc "OIDC discovery and tokens" "HTTPS"
        qbrain.server -> peer "Federation sync and proxy" "HTTPS"
        qbrain.subscriber -> qbrain.keyserver "Verifies manifests" "HTTP/JSON"

        # Server components (relationships traced from DI constructor dependencies, see evidence.md)
        agent -> qbrain.server.mcpHttp "MCP calls" "MCP HTTP /mcp-transport" "H"
        agent -> qbrain.server.stdioHost "MCP calls when launched with --transport stdio" "MCP STDIO" "H"
        qbrain.libs.client -> qbrain.server.middleware "REST requests" "HTTP/JSON" "H"
        qbrain.server.middleware -> qbrain.server.identity "Authenticates JWT bearer tokens (UseAuthentication)" "In-process" "M"
        qbrain.server.middleware -> qbrain.server.api "Authenticated, workspace-resolved requests" "In-process" "H"
        qbrain.server.middleware -> qbrain.server.workspace "Resolves workspace and populates WorkspaceContext" "In-process" "H"
        qbrain.server.middleware -> qbrain.server.pairing "Validates workspace tokens" "In-process" "H"
        qbrain.server.middleware -> qbrain.server.federation "Federation proxy and routing" "In-process" "H"
        qbrain.server.middleware -> qbrain.server.storage "Optional DbContext lookup for workspace resolution" "In-process" "H"
        qbrain.server.identity -> qbrain.db "Identity tables via separate McpIdentityDbContext (SQL Server connection string)" "EF Core" "M"
        qbrain.server.identity -> oidc "External OIDC (optional)" "HTTPS" "H"
        qbrain.server.stdioHost -> qbrain.server.mcpTools "Registers the same tool classes as the HTTP transport" "In-process" "H"
        qbrain.server.mcpHttp -> qbrain.server.mcpTools "Dispatches tool calls" "In-process" "H"
        qbrain.server.mcpTools -> qbrain.server.todo "Uses" "In-process" "H"
        qbrain.server.mcpTools -> qbrain.server.sessionLog "Uses" "In-process" "H"
        qbrain.server.mcpTools -> qbrain.server.requirements "Uses" "In-process" "H"
        qbrain.server.mcpTools -> qbrain.server.memory "Uses (via gated service)" "In-process" "H"
        qbrain.server.mcpTools -> qbrain.server.search "Uses" "In-process" "H"
        qbrain.server.mcpTools -> qbrain.server.triage "Uses" "In-process" "H"
        qbrain.server.mcpTools -> qbrain.server.workspace "Resolves workspace" "In-process" "H"
        qbrain.server.mcpTools -> qbrain.server.txsec "Mutations through gated services and turn coordinator" "In-process" "H"
        qbrain.server.mcpTools -> qbrain.server.graphrag "Uses" "In-process" "H"
        qbrain.server.mcpTools -> qbrain.server.github "Uses" "In-process" "H"
        qbrain.server.mcpTools -> qbrain.server.agents "Desktop launch" "In-process" "H"
        qbrain.server.mcpTools -> qbrain.server.tools "Prompt templates" "In-process" "H"
        qbrain.server.mcpTools -> qbrain.server.storage "Sync status and DbContext" "In-process" "H"
        qbrain.server.api -> qbrain.server.workspace "Resolves workspace, policy and validation" "In-process" "H"
        qbrain.server.api -> qbrain.server.txsec "Mutations go through gated services and turn coordinator" "In-process" "H"
        qbrain.server.api -> qbrain.server.todo "TODO queries, execution and prompts" "In-process" "H"
        qbrain.server.api -> qbrain.server.sessionLog "Session logs and transcript ingestion" "In-process" "H"
        qbrain.server.api -> qbrain.server.requirements "Requirements, use cases and recovery" "In-process" "H"
        qbrain.server.api -> qbrain.server.memory "Memory operations" "In-process" "H"
        qbrain.server.api -> qbrain.server.search "Context search, pack, repo files" "In-process" "H"
        qbrain.server.api -> qbrain.server.graphrag "GraphRAG queries and commands" "In-process" "H"
        qbrain.server.api -> qbrain.server.triage "Triage, handoff, hostile review" "In-process" "H"
        qbrain.server.api -> qbrain.server.brain "Brain slots and OpenAI-compatible quad brain endpoint" "In-process" "H"
        qbrain.server.api -> qbrain.server.agents "Agent pool and desktop launch" "In-process" "H"
        qbrain.server.api -> qbrain.server.federation "Federation endpoints" "In-process" "H"
        qbrain.server.api -> qbrain.server.tunnel "Tunnel control" "In-process" "H"
        qbrain.server.api -> qbrain.server.github "GitHub operations" "In-process" "H"
        qbrain.server.api -> qbrain.server.tools "Registry, templates, products" "In-process" "H"
        qbrain.server.api -> qbrain.server.pairing "Workspace token for QuadBrain endpoint" "In-process" "H"
        qbrain.server.api -> qbrain.server.storage "Direct DbContext use" "In-process" "H"
        qbrain.server.pairing -> qbrain.server.tools "Renders pairing pages from templates" "In-process" "H"
        qbrain.server.txsec -> qbrain.server.todo "Gated delegate" "In-process" "H"
        qbrain.server.txsec -> qbrain.server.sessionLog "Gated delegate" "In-process" "H"
        qbrain.server.txsec -> qbrain.server.memory "Gated delegate" "In-process" "H"
        qbrain.server.txsec -> qbrain.server.requirements "Gated delegate" "In-process" "H"
        qbrain.server.txsec -> qbrain.server.graphrag "Gated delegate" "In-process" "H"
        qbrain.server.txsec -> qbrain.server.workspace "Workspace scope for gated mutations" "In-process" "H"
        qbrain.server.txsec -> qbrain.server.tools "Gated delegate" "In-process" "H"
        qbrain.server.txsec -> qbrain.server.github "Gated delegate" "In-process" "H"
        qbrain.server.txsec -> qbrain.server.search "Gated delegate" "In-process" "H"
        qbrain.server.txsec -> qbrain.server.agents "Gated delegate" "In-process" "H"
        qbrain.server.txsec -> qbrain.server.storage "Audit and state via DbContext" "In-process" "H"
        qbrain.server.txsec -> qbrain.subscriber "Optional HTTP diffgram commit when TurnTransactionOptions.PubSubTransport=Http (default Direct, in-process)" "HTTP/JSON" "M"
        qbrain.server.todo -> qbrain.server.github "Issue sync" "In-process" "H"
        qbrain.server.todo -> qbrain.server.workspace "Workspace scope" "In-process" "H"
        qbrain.server.todo -> qbrain.server.requirements "Requirement linkage" "In-process" "H"
        qbrain.server.todo -> qbrain.server.federation "Federated TODO service" "In-process" "H"
        qbrain.server.todo -> qbrain.server.tools "Prompt templates" "In-process" "H"
        qbrain.server.todo -> qbrain.repoFs "Imports TODO.yaml" "File I/O" "H"
        qbrain.server.todo -> qbrain.server.storage "Persists" "In-process" "H"
        qbrain.server.sessionLog -> qbrain.server.storage "Persists" "In-process" "H"
        qbrain.server.sessionLog -> qbrain.server.workspace "Workspace scope" "In-process" "H"
        qbrain.server.sessionLog -> qbrain.server.federation "Federated session log service" "In-process" "H"
        qbrain.server.sessionLog -> qbrain.repoFs "Watches docs/sessions" "File I/O" "H"
        qbrain.server.requirements -> qbrain.server.storage "Persists" "In-process" "H"
        qbrain.server.requirements -> qbrain.server.workspace "Workspace scope" "In-process" "H"
        qbrain.server.memory -> qbrain.server.storage "Persists" "In-process" "H"
        qbrain.server.graphrag -> qbrain.server.storage "Persists" "In-process" "H"
        qbrain.server.graphrag -> qbrain.server.search "Hybrid search, embeddings, vector index" "In-process" "H"
        qbrain.server.graphrag -> qbrain.server.federation "Federated GraphRAG" "In-process" "H"
        qbrain.server.graphrag -> qbrain.server.workspace "Workspace scope" "In-process" "H"
        qbrain.server.search -> qbrain.server.storage "Persists FTS and ingestion state" "In-process" "H"
        qbrain.server.search -> qbrain.server.workspace "Workspace scope" "In-process" "H"
        qbrain.server.search -> qbrain.server.github "Issue and repo ingestion" "In-process" "H"
        qbrain.server.search -> qbrain.server.sessionLog "Session log ingestion" "In-process" "H"
        qbrain.server.search -> hf "Embedding model download" "HTTPS" "H"
        qbrain.server.search -> qbrain.vector "Index read/write" "File I/O" "H"
        qbrain.server.search -> qbrain.repoFs "Repo and docs ingestion" "File I/O" "H"
        qbrain.server.triage -> qbrain.server.storage "Persists" "In-process" "H"
        qbrain.server.triage -> qbrain.server.workspace "Workspace scope" "In-process" "H"
        qbrain.server.triage -> qbrain.server.todo "Creates TODOs" "In-process" "H"
        qbrain.server.triage -> qbrain.server.agents "Runs research through agent execution strategies" "In-process" "H"
        qbrain.server.triage -> qbrain.server.tools "Prompt templates" "In-process" "H"
        qbrain.server.triage -> qbrain.server.sessionLog "Sanitizes handoff content" "In-process" "H"
        qbrain.server.brain -> qbrain.server.txsec "Turn transactions and gated mutations" "In-process" "H"
        qbrain.server.brain -> qbrain.server.search "Context admission using chunker and embeddings" "In-process" "H"
        qbrain.server.brain -> qbrain.server.sessionLog "Logs brain interactions" "In-process" "H"
        qbrain.server.brain -> qbrain.server.todo "Internal tool: TODOs" "In-process" "H"
        qbrain.server.brain -> qbrain.server.requirements "Internal tool: requirements" "In-process" "H"
        qbrain.server.brain -> qbrain.server.graphrag "Internal tool: GraphRAG" "In-process" "H"
        qbrain.server.brain -> qbrain.server.agents "Internal tool: desktop launch" "In-process" "H"
        qbrain.server.brain -> qbrain.server.workspace "Workspace scope" "In-process" "H"
        qbrain.server.brain -> qbrain.server.storage "Persists" "In-process" "H"
        qbrain.server.brain -> llm "CLI or HTTP chat" "Process, HTTPS" "H"
        qbrain.server.agents -> qbrain.server.tools "Prompt templates" "In-process" "H"
        qbrain.server.agents -> qbrain.server.todo "Todo prompts" "In-process" "H"
        qbrain.server.agents -> qbrain.server.workspace "Workspace scope" "In-process" "H"
        qbrain.server.agents -> qbrain.server.pairing "Workspace token for hosted agents" "In-process" "H"
        qbrain.server.agents -> qbrain.server.storage "Persists" "In-process" "H"
        qbrain.server.agents -> llm "Runs agent CLIs" "Process" "H"
        qbrain.server.agents -> qbrain.launcher "Desktop spawn" "Process" "M"
        qbrain.server.federation -> qbrain.server.txsec "Applies operations inside turn transactions" "In-process" "H"
        qbrain.server.federation -> qbrain.server.todo "Push service reads TODOs" "In-process" "H"
        qbrain.server.federation -> qbrain.server.sessionLog "Push service reads session logs" "In-process" "H"
        qbrain.server.federation -> qbrain.server.storage "Outbox and ops" "In-process" "M"
        qbrain.server.federation -> peer "Sync, proxy, replay" "HTTPS" "H"
        qbrain.server.tunnel -> qbrain.server.workspace "Process manager for frpc" "In-process" "H"
        qbrain.server.tunnel -> tunnels "Provider clients" "Process" "H"
        qbrain.server.tunnel -> qbrain.frps "frpc" "frp" "H"
        qbrain.server.github -> qbrain.server.workspace "Workspace scope for tokens" "In-process" "H"
        qbrain.server.github -> github "gh CLI" "Process" "H"
        qbrain.server.workspace -> qbrain.server.storage "Persists" "In-process" "H"
        qbrain.server.workspace -> qbrain.server.pairing "Workspace tokens for process manager" "In-process" "H"
        qbrain.server.workspace -> qbrain.server.todo "Accessor resolves TODO service" "In-process" "H"
        qbrain.server.workspace -> qbrain.server.sessionLog "Policy writes session log" "In-process" "H"
        qbrain.server.workspace -> qbrain.server.federation "Process manager registers federation" "In-process" "H"
        qbrain.server.workspace -> qbrain.repoFs "Marker file and projections" "File I/O" "H"
        qbrain.server.storage -> qbrain.server.workspace "Workspace query filter" "In-process" "H"
        qbrain.server.storage -> qbrain.db "SQL" "EF Core" "H"
        qbrain.server.storage -> qbrain.server.mcpDbContext "Configures via provider strategies; applies migrations at startup" "In-process" "H"
        qbrain.server.mcpDbContext -> qbrain.db "SQL (SQLite, SQL Server or PostgreSQL)" "EF Core" "H"
        qbrain.server.mcpDbContext -> qbrain.server.workspace "Workspace foreign key and query filter (Workspace table)" "In-process" "H"
        qbrain.server.sqliteMigrations -> qbrain.db "Applies schema migrations (SQLite)" "EF Core Migrate" "H"
        qbrain.server.sqlServerMigrations -> qbrain.db "Applies schema migrations (SQL Server)" "EF Core Migrate" "H"
        qbrain.server.postgresMigrations -> qbrain.db "Applies schema migrations (PostgreSQL)" "EF Core Migrate" "H"
        qbrain.server.sqliteMigrations -> qbrain.server.mcpDbContext "Provides SQLite migrations and model snapshot" "In-process" "H"
        qbrain.server.sqlServerMigrations -> qbrain.server.mcpDbContext "Provides SQL Server migrations and model snapshot" "In-process" "H"
        qbrain.server.postgresMigrations -> qbrain.server.mcpDbContext "Provides PostgreSQL migrations and model snapshot" "In-process" "H"
        qbrain.server.identity -> qbrain.server.identityDbContext "ASP.NET Identity stores" "In-process" "H"
        qbrain.server.identityDbContext -> qbrain.db "Identity tables (SQL Server connection string; same database as main is configuration, unconfirmed)" "EF Core (SQL Server)" "M"
        qbrain.server.txsec -> qbrain.server.txDbContext "State store when a database path is configured" "In-process" "H"
        qbrain.server.txDbContext -> qbrain.txDb "SQL" "EF Core (SQLite)" "H"
        qbrain.server.tools -> qbrain.server.storage "Persists products" "In-process" "H"

        # REPL components
        agent -> qbrain.repl.stdio "YAML envelopes" "STDIO"
        qbrain.repl.stdio -> qbrain.repl.dispatcher "Parsed command" "In-process"
        qbrain.repl.dispatcher -> qbrain.repl.workflows "Dispatch" "In-process"
        qbrain.repl.workflows -> qbrain.libs.client "Calls" "In-process"
        qbrain.repl.trust -> qbrain.libs.client "Supplies endpoint and credentials" "In-process"
        qbrain.repl.trust -> qbrain.repoFs "Reads marker file" "File I/O"
        qbrain.libs.client -> qbrain.server "REST" "HTTP/JSON"

        # QBAgent components
        qbrain.qbagent.runLoop -> qbrain.libs.mcpAgent "Hosts agent" "In-process"
        qbrain.qbagent.runLoop -> qbrain.qbagent.agentTools "Registers tools" "In-process"
        qbrain.qbagent.agentTools -> qbrain.qbagent.agentSkills "References (skill tools)" "Project reference"
        qbrain.libs.mcpAgent -> qbrain.server "Todo and session-log workflows via client" "HTTP/JSON"
        qbrain.qbagent.runLoop -> llm "Chat" "HTTPS"

        # Key server components
        qbrain.keyserver.ksApi -> qbrain.keyserver.ksRegistry "Calls" "In-process"

        # Whole codebase (D2): library references (project references), loads, implementation and tooling links.
        # Evidence: csproj ProjectReference items and file headers (PLAN-C4MODEL-001 P3 inventory, 2026-10-19).
        qbrain.server.servicesLib -> qbrain.libs.mcpAgent "References" "Project reference"
        qbrain.server.servicesLib -> qbrain.server.storageLib "References" "Project reference"
        qbrain.server.servicesLib -> qbrain.server.cqrsLib "References" "Project reference"
        qbrain.server.servicesLib -> qbrain.server.cqrsMvvmLib "References" "Project reference"
        qbrain.server.servicesLib -> qbrain.libs.agentCli "References" "Project reference"
        qbrain.server.storageLib -> qbrain.libs.serviceDefaults "References" "Project reference"
        qbrain.server.storageLib -> qbrain.libs.agentCli "References" "Project reference"
        qbrain.server.storageLib -> qbrain.libs.client "References" "Project reference"
        qbrain.server.sqliteMigrations -> qbrain.server.storageLib "References" "Project reference"
        qbrain.server.sqlServerMigrations -> qbrain.server.storageLib "References" "Project reference"
        qbrain.server.postgresMigrations -> qbrain.server.storageLib "References" "Project reference"
        qbrain.server.graphrag -> qbrain.server.servicesLib "References" "Project reference"
        qbrain.server.graphrag -> qbrain.server.storageLib "References" "Project reference"
        qbrain.server.graphrag -> qbrain.server.cqrsLib "References" "Project reference"
        qbrain.server.graphrag -> qbrain.libs.agentCli "References" "Project reference"
        qbrain.server.cqrsMvvmLib -> qbrain.server.cqrsLib "References" "Project reference"
        qbrain.libs.mcpAgent -> qbrain.libs.client "References" "Project reference"
        qbrain.libs.mcpAgent -> qbrain.libs.replCore "References" "Project reference"
        qbrain.libs.replCore -> qbrain.libs.client "References" "Project reference"
        qbrain.libs.replCore -> qbrain.libs.agentCli "References" "Project reference"
        qbrain.qbagent.agentTools -> qbrain.libs.mcpAgent "References" "Project reference"
        qbrain.qbagent.agentTools -> qbrain.libs.agentCli "References" "Project reference"

        qbrain.server -> qbrain.libs.serviceDefaults "Loads" "Project reference"
        qbrain.server -> qbrain.libs.transactionSecurity "Loads" "Project reference"
        qbrain.server -> qbrain.libs.agentCli "Loads" "Project reference"
        qbrain.repl -> qbrain.libs.replCore "Loads" "Project reference"
        qbrain.repl -> qbrain.libs.agentCli "Loads" "Project reference"
        qbrain.repl -> qbrain.libs.client "Loads" "Project reference"
        qbrain.repl -> qbrain.libs.transactionSecurity "Loads" "Project reference"
        qbrain.qbagent -> qbrain.libs.mcpAgent "Loads" "Project reference"
        qbrain.keyserver -> qbrain.libs.serviceDefaults "Loads" "Project reference"
        qbrain.keyserver -> qbrain.libs.transactionSecurity "Loads" "Project reference"
        qbrain.subscriber -> qbrain.libs.serviceDefaults "Loads" "Project reference"
        qbrain.subscriber -> qbrain.libs.transactionSecurity "Loads" "Project reference"
        qbrain.sampleHost -> qbrain.libs.mcpAgent "Loads" "Project reference"
        qbrain.sampleHost -> llm "Chat completions" "OpenAI client"

        qbrain.server.todo -> qbrain.server.servicesLib "Implemented in" "Assembly"
        qbrain.server.sessionLog -> qbrain.server.servicesLib "Implemented in" "Assembly"
        qbrain.server.sessionLog -> qbrain.server.transcriptsLib "Transcript ingestion implemented in" "Assembly"
        qbrain.server.requirements -> qbrain.server.servicesLib "Implemented in" "Assembly"
        qbrain.server.memory -> qbrain.server.servicesLib "Implemented in" "Assembly"
        qbrain.server.search -> qbrain.server.servicesLib "Implemented in" "Assembly"
        qbrain.server.search -> qbrain.server.storageLib "Indexing implemented in" "Assembly"
        qbrain.server.triage -> qbrain.server.servicesLib "Implemented in" "Assembly"
        qbrain.server.workspace -> qbrain.server.servicesLib "Implemented in" "Assembly"
        qbrain.server.agents -> qbrain.server.servicesLib "Implemented in" "Assembly"
        qbrain.server.federation -> qbrain.server.servicesLib "Implemented in" "Assembly"
        qbrain.server.tunnel -> qbrain.server.servicesLib "Implemented in" "Assembly"
        qbrain.server.github -> qbrain.server.servicesLib "Implemented in" "Assembly"
        qbrain.server.tools -> qbrain.server.servicesLib "Implemented in" "Assembly"
        qbrain.server.storage -> qbrain.server.storageLib "Implemented in" "Assembly"
        qbrain.server.mcpDbContext -> qbrain.server.storageLib "Defined in" "Assembly"
        qbrain.server.txsec -> qbrain.libs.transactionSecurity "Implemented in" "Assembly"
        qbrain.server.txDbContext -> qbrain.libs.transactionSecurity "Defined in" "Assembly"
        qbrain.keyserver.ksRegistry -> qbrain.libs.transactionSecurity "Implemented in" "Assembly"
        qbrain.repl.dispatcher -> qbrain.libs.replCore "Implemented in" "Assembly"
        qbrain.repl.workflows -> qbrain.libs.replCore "Implemented in" "Assembly"

        qbrain.compat.compatClient -> qbrain.libs.client "Forwards to" "Type forwarding"
        qbrain.compat.compatCqrs -> qbrain.server.cqrsLib "Forwards to" "Type forwarding"
        qbrain.compat.compatCqrsMvvm -> qbrain.server.cqrsMvvmLib "Forwards to" "Type forwarding"
        qbrain.compat.compatMcpAgent -> qbrain.libs.mcpAgent "Forwards to" "Type forwarding"
        qbrain.compat.compatReplCore -> qbrain.libs.replCore "Forwards to" "Type forwarding"
        qbrain.compat.compatRepl -> qbrain.repl "Same tool under the 1.x command mcpserver-repl" "dotnet tool"
        qbrain.compat.compatQBAgent -> qbrain.qbagent "Same tool under the 1.x package id" "dotnet tool"

        qbrain.testSuites.testSupportMcp -> qbrain.server "Tests" "Project reference"
        qbrain.testSuites.testSupportMcp -> qbrain.testSuites.testProcessHelper "Starts as a child process" "Process"
        qbrain.testSuites.testSupportMcpIntegration -> qbrain.server "Tests in process" "WebApplicationFactory"
        qbrain.testSuites.testSupportMcpIntegration -> qbrain.repl "Tests" "Project reference"
        qbrain.testSuites.testSupportMcpIntegration -> qbrain.qbagent "Tests" "Project reference"
        qbrain.testSuites.testClient -> qbrain.libs.client "Tests" "Project reference"
        qbrain.testSuites.testCqrs -> qbrain.server.cqrsLib "Tests" "Project reference"
        qbrain.testSuites.testCqrs -> qbrain.server.cqrsMvvmLib "Tests" "Project reference"
        qbrain.testSuites.testLauncher -> qbrain.launcher "Tests" "Project reference"
        qbrain.testSuites.testMcpAgent -> qbrain.libs.mcpAgent "Tests" "Project reference"
        qbrain.testSuites.testQBAgent -> qbrain.qbagent "Tests" "Project reference"
        qbrain.testSuites.testReplCore -> qbrain.libs.replCore "Tests" "Project reference"
        qbrain.testSuites.testReplIntegration -> qbrain.repl "Tests" "Project reference"
        qbrain.testSuites.testAcid -> qbrain.keyserver "Tests" "Project reference"
        qbrain.testSuites.testAcid -> qbrain.subscriber "Tests" "Project reference"
        qbrain.testSuites.testTxSec -> qbrain.keyserver "Tests" "Project reference"
        qbrain.testSuites.testTxSec -> qbrain.subscriber "Tests" "Project reference"
        qbrain.testSuites.testTxSec -> qbrain.libs.transactionSecurity "Tests" "Project reference"
        qbrain.testSuites.testPluginIntegration -> qbrain.server "Tests plugin session-log flows" "Project reference"
        qbrain.testSuites.testPluginIntegration -> llm "AI assertions" "aiUnit"
        qbrain.testSuites.testPlanReview -> llm "AI plan review assertions" "aiUnit"
        qbrain.testSuites.testReview -> llm "AI code and project review assertions" "aiUnit"
        qbrain.testSuites.testBuild -> qbrain.nukeBuild "Tests" "Project reference"
        qbrain.testSuites.validationContext -> qbrain.server "Validates endpoints" "HTTP, port 7147"
        qbrain.testSuites.validationGitHub -> qbrain.server "Validates endpoints" "HTTP, port 7147"
        qbrain.testSuites.validationRepo -> qbrain.server "Validates endpoints" "HTTP, port 7147"
        qbrain.testSuites.validationSessionLog -> qbrain.server "Validates endpoints" "HTTP, port 7147"
        qbrain.testSuites.validationTodo -> qbrain.server "Validates endpoints" "HTTP, port 7147"
        qbrain.testSuites.validationToolRegistry -> qbrain.server "Validates endpoints" "HTTP, port 7147"
        qbrain.testSuites.validationWorkspace -> qbrain.server "Validates endpoints" "HTTP, port 7147"

        qbrain.nukeBuild -> qbrain.testSuites "Runs unit and Category=Integration tests (Test, MigrationIntegrationTests)" "dotnet test"
        qbrain.nukeBuild -> qbrain.testSuites.testReview "Runs (AiCodeReview, AiProjectReview, AiWarningSuppressionReview)" "dotnet test"
        qbrain.nukeBuild -> qbrain.testSuites.testPluginIntegration "Runs (PluginSessionLogIntegration)" "dotnet test"
        qbrain.nukeBuild -> qbrain.server "Compiles, packages and updates the Windows service (UpdateService)" "dotnet, PowerShell"

        qbrain.devTools.psContext -> qbrain.server "Context, session-log and TODO calls" "REST/JSON with API key"
        qbrain.devTools.bashHelpers -> qbrain.server "Session-log and TODO calls" "REST/JSON"
        qbrain.devTools.psRepl -> qbrain.repl "Invokes" "STDIO YAML"
        qbrain.devTools.tsRepl -> qbrain.repl "Runs qbrain-ai-repl --agent-stdio" "STDIO YAML"
        qbrain.devTools.memoryBench -> qbrain.server.servicesLib "References (memory services)" "Project reference"
        qbrain.devTools.unitGate -> qbrain.nukeBuild "Runs Nuke Test with one run id" "Process"
        qbrain.devTools.evidenceScripts -> qbrain.server "Gathers HTTP facts for reviewers" "HTTP"

        agent -> qbrain.plugins.pluginHosts "Triggers hooks" "Host hook scripts"
        qbrain.plugins.pluginHosts -> qbrain.plugins.pluginPs "Calls core" "PowerShell"
        qbrain.plugins.pluginHosts -> qbrain.plugins.pluginNode "Calls core (TS hosts)" "Node"
        qbrain.plugins.pluginHosts -> qbrain.plugins.pluginSh "Calls core (legacy)" "Bash"
        qbrain.plugins.pluginPs -> qbrain.repl "Starts and invokes via ensure-repl and repl-invoke" "Process, STDIO YAML"
        qbrain.plugins.pluginPs -> qbrain.devTools.psRepl "Uses" "PowerShell module"
        qbrain.plugins.pluginNode -> qbrain.devTools.tsRepl "Uses (Cline, Cline V2, OpenCode)" "npm package"
        qbrain.plugins.pluginSync -> qbrain.plugins.pluginHooks "Packages hooks and skills" "File copy"
        qbrain.plugins.pluginSync -> pluginRepos "Syncs the core into" "File copy"
        qbrain.plugins.agentCorePackage -> qbrain.repoFs "Reads and verifies the marker file" "File I/O"

        qbrain.opsScripts.opsService -> qbrain.server "Installs, updates and starts the service (Update-McpService)" "PowerShell"
        qbrain.opsScripts.opsIdentity -> oidc "Configures Keycloak realm, clients and users" "PowerShell, Bash"
        qbrain.opsScripts.opsPackaging -> qbrain.repl "Packs and installs the REPL tool" "dotnet pack, dotnet tool"
        qbrain.opsScripts.opsData -> qbrain.server "Index, TODO storage, encryption and requirements maintenance" "REST, CLI"
        qbrain.opsScripts.opsValidation -> qbrain.server "Config validation and smoke tests" "REST, PowerShell"
        qbrain.opsScripts.opsRebrand -> qbrain.repoFs "Rewrites product names in repository files (one-time)" "Python"

        # EF Core entity model (D5): Entity elements and foreign keys
        !include model/entities.dsl
        !include model/mkt.dsl
        !include model/qmd.dsl
        !include model/str.dsl

        # Deployment environments (evidence: see evidence.md, section Deployment)
        dockerEnv = deploymentEnvironment "DockerCompose" {
            dockerHost = deploymentNode "Docker host" "Any host running Docker Compose (docker-compose.mcp.yml)." "Docker Engine" {
                dockerContainerNode = deploymentNode "Container qbrain-ai" "Image qbrain-ai:latest built from Dockerfile (multi-stage on sdk:9.0 and aspnet:9.0 while the projects target net10.0; triage-report-1ce108ca70f34191a81a9927a5b3d512). Port 7147, /health healthcheck, restart unless-stopped, env_file .env." "Docker container" {
                    dockerServer = containerInstance qbrain.server
                }
                dockerVolumeNode = deploymentNode "Named volume qbrain-ai-data" "Mounted at /data. Holds mcp.db (Mcp__DataSource) and vector.idx (VectorIndex__IndexPath)." "Docker volume" {
                    dockerDb = containerInstance qbrain.db
                    dockerVector = containerInstance qbrain.vector
                }
                dockerBindNode = deploymentNode "Bind mount /workspace" "Host path from MCP_WORKSPACE (default current dir) mounted read-only at /workspace." "Docker bind mount (ro)" {
                    dockerRepo = containerInstance qbrain.repoFs
                }
            }
        }

        winEnv = deploymentEnvironment "WindowsService" {
            winHost = deploymentNode "Windows host" "Interactive Windows machine. Install path default C:\\ProgramData\\QBrainAi (scripts/Update-McpService.ps1)." "Windows" {
                winServiceNode = deploymentNode "Windows Service QBrainAi" "Service name from --service-name, default QBrainAi (Program.cs ResolveWindowsServiceName, UseWindowsService). Deployed only by scripts/Update-McpService.ps1 or build UpdateService; WindowsServiceDeploymentGuard checks .mcpservice-deployment.json hashes." "Windows Service (QBrainAi.Support.Mcp.exe)" {
                    winServer = containerInstance qbrain.server
                }
                winKeyServerNode = deploymentNode "Windows Service QBrainAiKeyServer" "Optional standalone key server host (QBrainAi.KeyServer Program.cs UseWindowsService)." "Windows Service" {
                    winKeys = containerInstance qbrain.keyserver
                }
                winSubscriberNode = deploymentNode "Subscriber process (optional)" "Console host of QBrainAi.Subscriber (Program.cs calls AddHttpTransactionSubscriber; no UseWindowsService). It is the only caller of the standalone key server (HttpKeyServerManifestService, KeyServerBaseUrl) and receives diffgram commits from the server when TurnTransactionOptions.PubSubTransport is Http." "Console process" {
                    winSubscriber = containerInstance qbrain.subscriber
                }
                winDesktopNode = deploymentNode "Interactive desktop session" "Needed because the launcher uses CreateProcessWithTokenW to start processes on the user desktop." "Windows desktop" {
                    winLauncher = containerInstance qbrain.launcher
                }
                winWorkspaceNode = deploymentNode "Workspace directories" "Repositories with AGENTS-README-FIRST markers, docs/Project/TODO.yaml and docs/sessions." "NTFS" {
                    winRepo = containerInstance qbrain.repoFs
                }
            }
        }

        linuxEnv = deploymentEnvironment "LinuxSystemd" {
            deploymentNode "Linux host" "Installer docs/setup/install-local-service.ps1 is written for one named host (PAYTON-OMARCHY, database McpServer_Omarchy). Treat as an example, not a generic target." "Linux with systemd" {
                deploymentNode "systemd unit mcpserver.service" "Runs /opt/mcpserver/app/McpServer.Support.Mcp --urls http://localhost:7147 as user mcpserver, EnvironmentFile /etc/mcpserver/mcpserver.env, Restart=on-failure." "systemd service" {
                    linuxServer = containerInstance qbrain.server
                }
                deploymentNode "Docker container sql-server-developer" "Existing SQL Server container; installer creates database McpServer_Omarchy and a dedicated login on 127.0.0.1,1433." "Docker container, SQL Server" {
                    linuxDb = containerInstance qbrain.db
                }
                deploymentNode "Workspace directory" "ACL-granted workspace /home/sharpninja/github/McpServer for the mcpserver account." "ext4 with POSIX ACLs" {
                    linuxRepo = containerInstance qbrain.repoFs
                }
            }
        }

        frpEnv = deploymentEnvironment "FrpRelay" {
            deploymentNode "Railway" "infra/frp/railway: service variables FRP_TOKEN, FRPS_BIND_PORT (7000), FRPS_VHOST_HTTP_PORT (8080); TCP proxy for the bind port; optional TCP range 7147-7160." "Railway" {
                deploymentNode "Container frps" "alpine:3.20 image with frps v0.61.1, entrypoint.sh renders frps.toml from frps.toml.template." "Docker container (infra/frp/railway/Dockerfile)" {
                    railwayFrps = containerInstance qbrain.frps
                }
            }
            deploymentNode "Local MCP host" "Runs the server with Mcp:Tunnel:Provider=frp; frpc started by FrpTunnelProvider." "Any host running the server" {
                frpServer = containerInstance qbrain.server
            }
        }

        frpLocalEnv = deploymentEnvironment "FrpRelayLocal" {
            deploymentNode "Docker host" "Local dev relay from infra/frp/local/docker-compose.frps.yml; publishes 7000 and 8080." "Docker Engine" {
                deploymentNode "Container frps" "Built from ../railway Dockerfile, restart unless-stopped." "Docker container" {
                    localFrps = containerInstance qbrain.frps
                }
            }
            deploymentNode "Local MCP host" "Server with frp tunnel provider." "Any host running the server" {
                localFrpServer = containerInstance qbrain.server
            }
        }
    }

    views {
        properties {
            # TR-MCP-NUKE-017 AC-2: no metadata line (title, description, render date) in exported SVG, so renders are byte-identical across runs.
            "structurizr.metadata" "false"
        }

        # Static C4 views (levels 1 to 3): neighbourhood views sized to the AC-5 letter page.
        !include views/c4.dsl
        !include views/mkt.dsl
        !include views/qmd.dsl
        !include views/str.dsl

        deployment qbrain "DockerCompose" "DeploymentDocker" "Deployment: Docker Compose container and its data volume (docker-compose.mcp.yml and Dockerfile)." {
            include dockerEnv.dockerHost.dockerContainerNode dockerEnv.dockerHost.dockerVolumeNode
            autolayout lr 100 50
        }

        deployment qbrain "DockerCompose" "DeploymentDockerWorkspace" "Deployment: Docker Compose container and the read-only workspace bind mount." {
            include dockerEnv.dockerHost.dockerContainerNode dockerEnv.dockerHost.dockerBindNode
            autolayout lr 100 50
        }

        deployment qbrain "WindowsService" "DeploymentWindowsService" "Deployment: Windows service host and the optional subscriber process (Update-McpService.ps1, UseWindowsService)." {
            include winEnv.winHost.winServiceNode winEnv.winHost.winSubscriberNode
            autolayout tb 100 50
        }

        deployment qbrain "WindowsService" "DeploymentWindowsKeyServer" "Deployment: optional subscriber process and the optional standalone key server service on Windows." {
            include winEnv.winHost.winSubscriberNode winEnv.winHost.winKeyServerNode
            autolayout tb 100 50
        }

        deployment qbrain "WindowsService" "DeploymentWindowsDesktop" "Deployment: Windows service with the desktop launcher session and the workspace directories." {
            include winEnv.winHost.winServiceNode winEnv.winHost.winDesktopNode winEnv.winHost.winWorkspaceNode
            autolayout lr 100 100
        }

        deployment qbrain "LinuxSystemd" "DeploymentLinuxSystemd" "Deployment: Linux systemd unit created by docs/setup/install-local-service.ps1." {
            include *
            autolayout lr 100 100
        }

        deployment qbrain "FrpRelay" "DeploymentFrpRailway" "Deployment: FRP relay on Railway (infra/frp/railway)." {
            include *
            autolayout lr 100 100
        }

        deployment qbrain "FrpRelayLocal" "DeploymentFrpLocal" "Deployment: FRP relay via local Docker Compose (infra/frp/local)." {
            include *
            autolayout lr 100 100
        }

        !include views/entities.dsl

        styles {
            # TR-MCP-NUKE-017 AC-5 (amended 2026-10-19): names render at 1.4 x fontSize, descriptions and labels at
            # 1 x, and the metadata line stays visible at 0.7 x (hiding it is an inspection error): a 2:1 spread
            # that fits the 8 to 16 pt band.
            element "Element" {
                fontSize 24
            }
            relationship "Relationship" {
                fontSize 24
            }
            element "Person" {
                shape person
                background #08427b
                color #ffffff
            }
            element "Software System" {
                background #1168bd
                color #ffffff
            }
            element "External" {
                background #999999
                color #ffffff
            }
            element "Container" {
                background #438dd5
                color #ffffff
            }
            element "Database" {
                shape cylinder
            }
            element "Component" {
                background #85bbf0
                color #000000
            }
            # PLAN-C4MODEL-001 D3 and D5 element kinds for the converted documentation diagrams (P4).
            element "Planned" {
                border dashed
            }
            element "Activity" {
                height 220
                background #f2f2f2
                color #000000
            }
            element "Start" {
                shape ellipse
                background #c8e6c9
            }
            element "Action" {
                shape roundedbox
            }
            element "Decision" {
                shape diamond
                background #fff3cd
            }
            element "Error" {
                background #f8d7da
                stroke #a94442
            }
            element "End" {
                shape ellipse
                background #d6d8db
            }
            element "State" {
                shape roundedbox
                height 220
                background #e7f1ff
                color #000000
            }
            element "Topic" {
                shape ellipse
                height 220
                background #f2f2f2
                color #000000
            }
            element "Entity" {
                # Entity text is short (keys and column count), so a lower box lets more entities fit the AC-5 page.
                height 220
                background #dde8f0
                color #000000
                stroke #5a7a94
            }
        }
    }

    configuration {
        scope softwaresystem
    }
}
