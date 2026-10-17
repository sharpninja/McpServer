workspace "QBrain.AI (McpServer) - reverse engineered from origin/develop" "C4 model derived from code evidence at commit 36783b79d061303bfb3e17abe685640a953491af. See evidence.md." {

    !identifiers hierarchical

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

        qbrain = softwareSystem "QBrain.AI" "Workspace-scoped context, memory, TODO, requirements, session-log, triage and agent-orchestration platform for AI coding agents (formerly McpServer)." {

            server = container "MCP Context Server" "Single ASP.NET Core host exposing REST, MCP HTTP transport, pairing UI, identity and background workers." "ASP.NET Core, .NET 9 (QBrainAi.Support.Mcp)" "Server" {
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
                graphrag = component "GraphRAG" "CQRS commands and queries, internal fallback or external-command backend, federated GraphRAG, global corpus seeder." "QBrainAi.GraphRag"
                triage = component "Triage, Handoff and Hostile Review" "Triage queue worker and research runner, handoff ingestion, hostile review director and service." "QBrainAi.Services"
                brain = component "Brain Slots and Quad Brain" "Brain slot registry, credential resolution, CLI and OpenAI chat clients, quad-brain orchestration and OpenAI-compatible controller." "Support.Mcp/Services, config/brain-slots"
                agents = component "Agent Pool and Processes" "Agent pool, health monitor, process manager, execution strategies (codex, copilot, grok, hosted, one-shot), branch and isolation strategies." "QBrainAi.Services, QBrainAi.McpAgent hosting"
                federation = component "Federation" "Registry, topology, envelope signing, proxy, queued replay, fanout sync, conflict and outbox." "QBrainAi.Services (Federation*)"
                tunnel = component "Tunnel Registry" "Starts and tracks Cloudflare, ngrok or FRP tunnels." "QBrainAi.Services (TunnelRegistry, *TunnelProvider)"
                github = component "GitHub Integration" "gh CLI wrapper, workspace token store, issue-todo sync." "GitHubCliService"
                txsec = component "Transaction Security" "Turn transaction coordinator, diffgram protector, pub/sub replay worker, gated service decorators (TransactionGated*)." "QBrainAi.TransactionSecurity"
                tools = component "Tool Registry, Templates and Products" "Tool registry and buckets, prompt templates, products and workspace sharing." "QBrainAi.Services, Support.Mcp/Products"
                storage = component "Storage and Providers" "EF Core DbContext, provider strategies for SQLite, PostgreSQL, SQL Server, migration and encryption coordinators, startup bootstrap." "QBrainAi.Storage and *Migrations"
            }

            db = container "MCP Database" "Relational store for TODOs, session logs, requirements, memory, graph, federation, identity and audit data. Default file mcp.db in /data." "SQLite (default), PostgreSQL or SQL Server via EF Core" "Database"
            vector = container "Vector Index" "HNSW index file used for semantic search, at /data/vector.idx." "HnswIndex file, ONNX embeddings" "Database"
            repoFs = container "Workspace Files" "Mounted repository: AGENTS-README-FIRST marker, docs/Project/TODO.yaml, docs/sessions, requirements docs." "Filesystem (/workspace)" "Database"
            keyserver = container "Key Server" "Optional standalone host of the in-memory key server (InMemoryKeyServerService): registers parties, serves public keys, signs and verifies transaction manifests. The MCP Context Server and REPL embed the same services in-process by default." "ASP.NET Core minimal API (QBrainAi.KeyServer)" "Server" {
                ksApi = component "Key Server Endpoints" "POST /qbrainai/keyserver/parties, GET parties/{id}/keys/{id}, POST manifests/sign and verify, GET manifests/report." "Minimal APIs"
                ksRegistry = component "Party Registry and Manifest Service" "IKeyServerPartyRegistry, IKeyServerManifestService, key provisioning." "QBrainAi.TransactionSecurity"
            }
            subscriber = container "Transaction Subscriber" "Optional standalone subscriber host (AddHttpTransactionSubscriber): receives diffgram commits over HTTP and verifies manifests through the Key Server. Default server configuration uses an in-process subscriber instead." "ASP.NET Core minimal API (QBrainAi.Subscriber)" "Server"
            repl = container "REPL Host" "Per-agent YAML-over-STDIO and interactive REPL that proxies workflows (todo, session log, memory, requirements, GraphRAG, handoff, triage) to the server REST API." "Console app, .NET 9, System.CommandLine (QBrainAi.Repl.Host)" "CLI" {
                stdio = component "Agent STDIO Handler" "Reads YAML envelopes on STDIN, writes responses; modes --agent-stdio and --interactive." "AgentStdioHandler, AgentStdioProtocol"
                dispatcher = component "REPL Command Dispatcher" "Routes commands to workflows, validates YAML messages, mutation policy." "QBrainAi.Repl.Core"
                workflows = component "Workflows" "Todo, SessionLog, Memory, Requirements, GraphRag, Handoff, HostileReview, Triage, WorkspaceValidation, AgentHelp, TranscriptIngestion workflows." "QBrainAi.Repl.Core"
                trust = component "Marker and Trust Bootstrap" "Finds AGENTS-README-FIRST marker, resolves workspace and client options, trust state, auth rotation." "MarkerFileClientOptionsResolver, TrustBootstrapService"
                client = component "QBrainAi.Client" "Typed REST client (about 40 clients: Todo, SessionLog, Context, Memory, GraphRag, Federation, Tunnel, ...)." "HttpClient library"
            }
            plugins = container "Agent Plugin Core" "Shared hook, marker-trust, cache and session-log libraries synced into each mcpserver-*-plugin repo for claude-code, codex, copilot, grok, cline, opencode hosts." "Bash, PowerShell, Node (plugins/core)" "CLI"
            qbagent = container "QBAgent" "Standalone interactive agent runtime with run loop, chat client, tools and skills, using McpAgent hosting and the client library." "Console app, .NET 9, Microsoft.Agents.AI (QBrainAi.QBAgent)" "CLI" {
                runLoop = component "QBAgent Run Loop" "Bootstrapper, run loop, progress and session-logging chat clients." "QBrainAi.QBAgent"
                agentTools = component "QBAgent Tools and Skills" "Bash, PowerShell, Git, file and skill tools." "QBrainAi.QBAgent.Tools, QBrainAi.QBAgent.Skills"
                mcpAgent = component "McpAgent Hosting" "Hosted agent runtime, PowerShell session manager, session-log and todo workflows." "QBrainAi.McpAgent"
            }
            launcher = container "Desktop Launcher" "Windows helper that launches a process on the interactive desktop using CreateProcessWithTokenW; takes JSON argument and returns JSON on stdout." "Console exe, Win32 P/Invoke (QBrainAi.Launcher)" "CLI"
            frps = container "FRP Relay" "Self-hosted frps server (Railway or local compose) tunnelling the server port." "frp, Docker (infra/frp)" "Infrastructure"
        }


        # Deployment environments (evidence: see evidence.md, section Deployment)
        dockerEnv = deploymentEnvironment "DockerCompose" {
            deploymentNode "Docker host" "Any host running Docker Compose (docker-compose.mcp.yml)." "Docker Engine" {
                deploymentNode "Container qbrain-ai" "Image qbrain-ai:latest built from Dockerfile (multi-stage, aspnet:9.0). Port 7147, /health healthcheck, restart unless-stopped, env_file .env." "Docker container" {
                    dockerServer = containerInstance qbrain.server
                }
                deploymentNode "Named volume qbrain-ai-data" "Mounted at /data. Holds mcp.db (Mcp__DataSource) and vector.idx (VectorIndex__IndexPath)." "Docker volume" {
                    dockerDb = containerInstance qbrain.db
                    dockerVector = containerInstance qbrain.vector
                }
                deploymentNode "Bind mount /workspace" "Host path from MCP_WORKSPACE (default current dir) mounted read-only at /workspace." "Docker bind mount (ro)" {
                    dockerRepo = containerInstance qbrain.repoFs
                }
            }
        }

        winEnv = deploymentEnvironment "WindowsService" {
            deploymentNode "Windows host" "Interactive Windows machine. Install path default C:\\ProgramData\\QBrainAi (scripts/Update-McpService.ps1)." "Windows" {
                deploymentNode "Windows Service QBrainAi" "Service name from --service-name, default QBrainAi (Program.cs ResolveWindowsServiceName, UseWindowsService). Deployed only by scripts/Update-McpService.ps1 or build UpdateService; WindowsServiceDeploymentGuard checks .mcpservice-deployment.json hashes." "Windows Service (QBrainAi.Support.Mcp.exe)" {
                    winServer = containerInstance qbrain.server
                }
                deploymentNode "Windows Service QBrainAiKeyServer" "Optional standalone key server host (QBrainAi.KeyServer Program.cs UseWindowsService)." "Windows Service" {
                    winKeys = containerInstance qbrain.keyserver
                }
                deploymentNode "Interactive desktop session" "Needed because the launcher uses CreateProcessWithTokenW to start processes on the user desktop." "Windows desktop" {
                    winLauncher = containerInstance qbrain.launcher
                }
                deploymentNode "Workspace directories" "Repositories with AGENTS-README-FIRST markers, docs/Project/TODO.yaml and docs/sessions." "NTFS" {
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
        qbrain.repl.client -> qbrain.server.middleware "REST requests" "HTTP/JSON" "H"
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
        qbrain.server.tools -> qbrain.server.storage "Persists products" "In-process" "H"

        # REPL components
        agent -> qbrain.repl.stdio "YAML envelopes" "STDIO"
        qbrain.repl.stdio -> qbrain.repl.dispatcher "Parsed command" "In-process"
        qbrain.repl.dispatcher -> qbrain.repl.workflows "Dispatch" "In-process"
        qbrain.repl.workflows -> qbrain.repl.client "Calls" "In-process"
        qbrain.repl.trust -> qbrain.repl.client "Supplies endpoint and credentials" "In-process"
        qbrain.repl.trust -> qbrain.repoFs "Reads marker file" "File I/O"
        qbrain.repl.client -> qbrain.server "REST" "HTTP/JSON"

        # QBAgent components
        qbrain.qbagent.runLoop -> qbrain.qbagent.mcpAgent "Hosts agent" "In-process"
        qbrain.qbagent.runLoop -> qbrain.qbagent.agentTools "Registers tools" "In-process"
        qbrain.qbagent.mcpAgent -> qbrain.server "Todo and session-log workflows via client" "HTTP/JSON"
        qbrain.qbagent.runLoop -> llm "Chat" "HTTPS"

        # Key server components
        qbrain.keyserver.ksApi -> qbrain.keyserver.ksRegistry "Calls" "In-process"
    }

    views {
        systemContext qbrain "SystemContext" "C4 level 1: QBrain.AI and its users and external systems." {
            include *
            autolayout lr
        }

        container qbrain "Containers" "C4 level 2: deployable and runtime units of QBrain.AI." {
            include *
            autolayout lr
        }

        component qbrain.server "ServerComponents" "C4 level 3: MCP Context Server (QBrainAi.Support.Mcp plus referenced libraries)." {
            include *
            autolayout tb
        }

        component qbrain.repl "ReplComponents" "C4 level 3: REPL Host." {
            include *
            autolayout lr
        }

        component qbrain.qbagent "QBAgentComponents" "C4 level 3: QBAgent." {
            include *
            autolayout lr
        }

        component qbrain.keyserver "KeyServerComponents" "C4 level 3: Key Server." {
            include *
            autolayout lr
        }

        deployment qbrain "DockerCompose" "DeploymentDocker" "Deployment: Docker Compose (docker-compose.mcp.yml and Dockerfile)." {
            include *
            autolayout lr
        }

        deployment qbrain "WindowsService" "DeploymentWindowsService" "Deployment: Windows service (Update-McpService.ps1, UseWindowsService)." {
            include *
            autolayout lr
        }

        deployment qbrain "LinuxSystemd" "DeploymentLinuxSystemd" "Deployment: Linux systemd unit created by docs/setup/install-local-service.ps1." {
            include *
            autolayout lr
        }

        deployment qbrain "FrpRelay" "DeploymentFrpRailway" "Deployment: FRP relay on Railway (infra/frp/railway)." {
            include *
            autolayout lr
        }

        deployment qbrain "FrpRelayLocal" "DeploymentFrpLocal" "Deployment: FRP relay via local Docker Compose (infra/frp/local)." {
            include *
            autolayout lr
        }

        styles {
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
        }
    }
}
