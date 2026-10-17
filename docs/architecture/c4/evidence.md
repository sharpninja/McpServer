# Evidence for workspace.dsl

Source: origin/develop of github.com/sharpninja/McpServer at commit 36783b79d061303bfb3e17abe685640a953491af (fetched 2026-10-17). All paths are repo-relative; the local checkout is /home/sharpninja/github/McpServer on LAB-OMARCHY. Evidence was read from a `git archive` copy in /tmp/c4src and /tmp/c4x, not from the working checkouts. Confidence: H = directly seen in code or config, M = inferred from names or project references, L = guess.

## Actors
- Developer / Operator (H): Swagger UI redirect and `/pair` pages in src/QBrainAi.Support.Mcp/Program.cs (lines 929, 997-1075); interactive mode in src/QBrainAi.Repl.Host/Program.cs.
- AI Coding Agent (H): src/QBrainAi.Repl.Host/MarkerFileClientOptionsResolver.cs (claude, codex, copilot, grok agent names); plugins/core/hosts/{claude-code,claude-cowork,cline,cline-v2,codex,copilot,grok,opencode}; plugins/core/README.md.

## External systems
- GitHub (H): src/QBrainAi.Services/Services/GitHubCliService.cs (`gh` executable, line 17); Controllers/GitHubController.cs; Ingestion/GitHubIngestor.cs, IssueIngestor.cs; Services/IssueTodoSyncService.cs.
- LLM Providers and Agent CLIs (H): config/brain-slots/quad-brain-slot-assignments.yaml (ProviderKind Cli, endpoints cli://grok-cli and cli://codex-cli); Services/{Codex,Copilot,Grok}CliAgentExecutionStrategy.cs; Support.Mcp/Services/CliBrainSlotChatClient.cs, BrainSlotChatClientFactory.cs; Support.Mcp csproj packages Microsoft.Extensions.AI.OpenAI and OpenAI; src/QBrainAi.QBAgent/QBAgentChatClientFactory.cs (M for HTTP details).
- Tunnel Providers (H): Services/CloudflareTunnelProvider.cs, NgrokTunnelProvider.cs, FrpTunnelProvider.cs, TunnelRegistry.cs; infra/frp/railway/README.md (`Mcp:Tunnel:Provider = "frp"`).
- Parseable (H): Support.Mcp/Logging/Parseable*.cs; Services/Options/McpParseableOptions.cs (optional, default off); Program.cs lines 160-163 (sink wiring, partly commented).
- Hugging Face (H): src/QBrainAi.Storage/Indexing/EmbeddingService.cs lines 18-19 (model.onnx and vocab.txt URLs); Dockerfile `Embedding__AutoDownload=true`.
- External OIDC Provider (H): Support.Mcp/Identity/ExternalOidcProviderStrategy.cs, OidcProviderStrategyFactory.cs, DisabledOidcProviderStrategy.cs.
- Federated QBrain.AI Instance (H): Services/Federation*.cs, FederationProxyService.cs (HttpClient at Program.cs line 600), Support.Mcp/Middleware/FederationMiddleware.cs, Storage/Entities/Federation*Entity.cs, hosted services at Program.cs lines 710-712.

## Containers
- MCP Context Server (H): src/QBrainAi.Support.Mcp/QBrainAi.Support.Mcp.csproj (Sdk.Web); Dockerfile publishes it and exposes 7147 with /health; Program.cs line 185 (UseUrls), 776-777 (AddMcpServer, WithHttpTransport), 993 (MapMcp "/mcp-transport"), 995 (MapControllers), 127 (UseWindowsService); hosted services lines 339 and 705-728.
- MCP Database (H): docker-compose.mcp.yml and Dockerfile (`Mcp__DataSource=mcp.db`, `Mcp__DataDirectory=/data`, volume qbrain-ai-data); src/QBrainAi.Storage/Database/{Sqlite,PostgreSql,SqlServer}McpDatabaseProviderStrategy.cs; src/QBrainAi.Storage.{Sqlite,PostgreSql,SqlServer}Migrations.
- Vector Index (H): Dockerfile `VectorIndex__IndexPath=/data/vector.idx`; src/QBrainAi.Storage/Indexing/VectorIndexService.cs, VectorIndexStartupService.cs.
- Workspace Files (H): Dockerfile and docker-compose.mcp.yml (`/workspace` mount read-only, `Mcp__RepoRoot`, `Mcp__TodoFilePath=docs/Project/TODO.yaml`, `Mcp__SessionsPath=docs/sessions`); Ingestion/SessionLogFileWatcher.cs; Services/TodoBootstrapImporter.cs.
- Key Server (H): src/QBrainAi.KeyServer/Program.cs (routes under /qbrainai/keyserver/*, Windows service name QBrainAiKeyServer). Its database use is M (TransactionSecurity references EF Sqlite, TransactionSecurityStateStores.cs).
- Transaction Subscriber (H): src/QBrainAi.Subscriber/Program.cs (routes /qbrainai/subscriber/diffgrams/commit and transactions/{id}/status|abort).
- REPL Host (H): src/QBrainAi.Repl.Host/Program.cs (`--agent-stdio`, `--interactive`, `--workspace-path`, `--marker-file`, `--agent`); src/QBrainAi.Repl.Core (55 files).
- Agent Plugin Core (H): plugins/core/README.md, plugins/core/lib-sh, lib-ps, lib-node, hooks-templates, hosts, sync. The separate mcpserver-*-plugin repos are not in this repo (M).
- QBAgent (H): src/QBrainAi.QBAgent (OutputType Exe, Program.cs, QBAgentRunLoop.cs), QBrainAi.QBAgent.Tools, QBrainAi.QBAgent.Skills, QBrainAi.McpAgent; sample: src/QBrainAi.McpAgent.SampleHost.
- Desktop Launcher (H): src/QBrainAi.Launcher/Program.cs and Native/*; invoked by Support.Mcp Services/DesktopProcessSpawner.cs and DesktopLaunchService.cs (call link is M).
- FRP Relay (H): infra/frp/railway/{Dockerfile,entrypoint.sh,frps.toml.template}, infra/frp/local/docker-compose.frps.yml.

## Server components (all under src/)
- REST Controllers: QBrainAi.Support.Mcp/Controllers/*.cs (36 files). H.
- MCP HTTP Transport: Program.cs 776-777, 993. H.
- MCP Tool Surface: QBrainAi.Support.Mcp/McpStdio/{FwhMcpTools.*.cs,QBrainAiMcpTools.cs,McpStdioHost.cs}. H that tools exist; M that the HTTP transport serves the same tools.
- Pairing and API Key: Program.cs 948, 997-1075; Services/{PairingSessionService,PairingLoginAttemptGuard,WorkspaceTokenService,ApiKeyIssuanceGuard}.cs. H.
- Request Pipeline: Support.Mcp/Middleware/*.cs; Program.cs 565-600 (JWT bearer, policies), 607 (health checks), 926-927 (Swagger). H.
- Identity: Support.Mcp/Identity/*; packages Duende.IdentityServer*, Microsoft.AspNetCore.Identity.EntityFrameworkCore. H.
- Workspace Service: Services/{WorkspaceService,WorkspaceProcessManager,WorkspacePolicyService,WorkspaceProjectionWriter,MarkerFileService}.cs. H.
- TODO Service: Services/{EfTodoService,TodoExecutionService,TodoCreationService,TodoUpdateService,TodoBootstrapImporter,LegacyTodoSqliteMigrator}.cs. H.
- Session Log Service: Services/SessionLog*.cs; Ingestion/SessionLogIngestor.cs; src/QBrainAi.SessionLog.Transcripts. H.
- Requirements and Use Cases: QBrainAi.Services/Requirements (15 files), UseCases (33 files); Controllers/{Requirements,RequirementsRecovery,UseCases}Controller.cs. H.
- Memory Service: QBrainAi.Services/Memory (54 files); Controllers/MemoryController.cs. H.
- Context Search and Ingestion: Services/{Fts5SearchService,HybridSearchService}.cs; Ingestion/*; Storage/Indexing/*. H.
- GraphRAG: src/QBrainAi.GraphRag (Commands, Queries, Services/{GraphRagService,ExternalCommandGraphRagBackendAdapter,InternalFallbackGraphRagBackendAdapter,FederatedGraphRagService}.cs); Support.Mcp/Services/GraphRagGlobalCorpusStartupSeeder.cs. H.
- Triage, Handoff and Hostile Review: Services/{TriageService,TriageQueueWorker,ConfiguredTriageResearchRunner,Handoff*,HostileReview*}.cs. H.
- Brain Slots and Quad Brain: Support.Mcp/Services/{BrainSlot*,QuadBrain*,CliBrainSlot*}.cs; Controllers/{BrainSlots,QuadBrainOpenAi}Controller.cs. H.
- Agent Pool and Processes: Services/{AgentPoolService,AgentProcessManager,AgentHealthMonitorService,*AgentExecutionStrategy,*AgentBranchStrategy,*AgentIsolationStrategy}.cs; QBrainAi.McpAgent/Hosting. H.
- Federation: Services/Federation*.cs; Controllers/FederationController.cs. H.
- Tunnel Registry: Services/TunnelRegistry.cs and providers. H.
- GitHub Integration: Services/{GitHubCliService,FileGitHubWorkspaceTokenStore,IssueTodoSyncService}.cs. H.
- Transaction Security: src/QBrainAi.TransactionSecurity/Services/*; Support.Mcp/Services/TransactionGated*.cs; Program.cs line 713 (TransactionPubSubReplayWorker). H. Server to KeyServer/Subscriber links are M (HttpKeyServerManifestService.cs, AddHttpTransactionSubscriber exist; which mode the server runs is configuration).
- Tool Registry, Templates and Products: Services/{ToolRegistryService,ToolBucketService,PromptTemplateService}.cs; Support.Mcp/Products/*; templates/prompt-templates.yaml. H.
- Storage and Providers: src/QBrainAi.Storage (Database, Entities 71 files, Migrations); csproj references to the three migration projects in Support.Mcp. H.

## REPL Host components
Agent STDIO Handler, Command Dispatcher, Workflows: src/QBrainAi.Repl.Core/{AgentStdioProtocol,ReplCommandDispatcher,*Workflow}.cs. Marker and Trust: src/QBrainAi.Repl.Host/MarkerFileClientOptionsResolver.cs, Repl.Core/IMarkerFileReader.cs, ITrustBootstrapService.cs. Client: src/QBrainAi.Client (40+ *Client.cs). H.

## Project reference evidence (csproj)
- Support.Mcp references ServiceDefaults, TransactionSecurity, Common.AgentCli, Storage, three Storage migration projects, Services, SessionLog.Transcripts, GraphRag.
- Services references McpAgent, Storage, Cqrs, Cqrs.Mvvm, Common.AgentCli.
- GraphRag references Services, Storage, Cqrs, Common.AgentCli.
- Storage references ServiceDefaults, Common.AgentCli, Client.
- Repl.Host references Repl.Core, Common.AgentCli, Client, TransactionSecurity.
- Repl.Core and McpAgent reference Client; McpAgent also Repl.Core.
- QBAgent references McpAgent and QBAgent.Tools; QBAgent.Tools references McpAgent, Common.AgentCli, QBAgent.Skills.
- KeyServer and Subscriber reference ServiceDefaults and TransactionSecurity.
- Launcher has no project references.
- Src/Compatibility/SharpNinja.McpServer.* are legacy-name compatibility shims, deliberately not modeled.

## Deliberately not modeled
Tests (tests/), build (build/, Nuke), docs/receipts, lib/NSubstitute, tools/MemoryBenchLiveSubscription, tools/McpServerTools submodule, Compatibility shims, and library-only projects (Cqrs, Cqrs.Mvvm, ServiceDefaults, Common.AgentCli) except as noted in component descriptions.

## Step A: traced relationships (DI and constructor dependencies)

Method: scanned every constructor and primary constructor in QBrainAi.Support.Mcp, Services, Storage, GraphRag, TransactionSecurity, SessionLog.Transcripts and McpAgent (git archive copy of commit 36783b79), mapped each dependency type to the component that owns its source file, then cross-checked Program.cs registrations (AddSingleton/AddScoped/AddHostedService, MapMcp, MapControllers, middleware order at lines 904-921). H = a constructor parameter, DI registration or direct call site was read. M = inferred from naming, configuration or one-sided evidence. Script: /tmp/c4work/trace.py on LAB-OMARCHY.

Corrections to the earlier model:
- Server to Key Server over HTTP removed (H). Program.cs:214 calls AddInProcessTransactionSecurity, which registers InMemoryKeyServerService and InMemorySubscriberCommitService inside the server process. The Key Server host (QBrainAi.KeyServer/Program.cs) is a standalone optional host of the same in-memory service.
- REPL to Key Server removed (H). Repl.Host/Program.cs:91 also calls AddInProcessTransactionSecurity.
- Key Server to database removed (H). KeyServer references only ServiceDefaults and TransactionSecurity; the service is in memory. TransactionSecurity uses SQLite only for the optional pub/sub broker store (SqliteTransactionSecurityStateStore, only when TurnTransactionOptions.PubSubDatabasePath is set).
- Server to Subscriber HTTP kept but marked optional (M): TurnTransactionOptions.PubSubTransport defaults to Direct (in-process); Http and ExternalBroker are options.
- Added Subscriber to Key Server HTTP (H): AddHttpTransactionSubscriber registers HttpKeyServerManifestService with SubscriberOptions.KeyServerBaseUrl (default http://localhost:7167).
- Triage to Brain Slots removed (no constructor or call evidence). Triage uses agent execution strategies (ConfiguredTriageResearchRunner) and the agent pool (HandoffOneShotExtractor).
- Identity to shared storage replaced by Identity to database via its own McpIdentityDbContext, configured with UseSqlServer and a separate connection string (M: the exact relation to the main database is not traced).
- MCP STDIO is a mode of the same executable (Program.cs:50-52, McpStdioHost.cs), not a separate container. Both transports call WithToolsFromAssembly(typeof(FwhMcpTools).Assembly), so the tool set is identical (H). New component MCP STDIO Host added.
- /mcp-transport passes through WorkspaceAuthMiddleware without token validation (WorkspaceAuthMiddleware only protects /qbrainai/* and /mcpserver/* aliases) (H).

Relationship list (format: source -> destination [confidence] : evidence):
- agent -> qbrain.server.mcpHttp [H]: Program.cs:777,993 (WithHttpTransport, MapMcp)
- agent -> qbrain.server.stdioHost [H]: Program.cs:50-52 (IsStdioTransportRequested -> McpStdioHost.RunAsync); McpStdioHost.cs:332
- qbrain.repl.client -> qbrain.server.middleware [H]: QBrainAi.Client typed clients; server pipeline Program.cs:904-921
- middleware -> identity [M]: Program.cs:917 UseAuthentication, JwtBearer config 565-600; no direct constructor dependency
- middleware -> api [H]: Program.cs:904-921 then MapControllers 995
- middleware -> workspace [H]: WorkspaceResolutionMiddleware.InvokeAsync(IWorkspaceService, WorkspaceContext)
- middleware -> pairing [H]: WorkspaceResolutionMiddleware and WorkspaceAuthMiddleware take WorkspaceTokenService
- middleware -> federation [H]: FederationMiddleware ctor (FederationRegistry, FederationProxyService)
- middleware -> storage [H]: WorkspaceResolutionMiddleware.InvokeAsync(McpDbContext? dbContext = null)
- identity -> qbrain.db [M]: Identity/IdentityServerExtensions.cs (AddDbContext<McpIdentityDbContext>, UseSqlServer); not shared McpDbContext
- identity -> oidc [H]: Identity/ExternalOidcProviderStrategy.cs, OidcProviderStrategyFactory.cs
- stdioHost -> mcpTools [H]: McpStdioHost.cs:332-333 and Program.cs:778 both call WithToolsFromAssembly(typeof(FwhMcpTools).Assembly)
- mcpHttp -> mcpTools [H]: Program.cs:776-778 WithToolsFromAssembly
- mcpTools -> todo [H]: QBrainAiMcpTools ctor: TodoServiceResolver, TodoCreationService, ITodoPromptService
- mcpTools -> sessionLog [H]: QBrainAiMcpTools ctor: ISessionLogService, ITranscriptIngestionService
- mcpTools -> requirements [H]: QBrainAiMcpTools ctor: IRequirementsDocumentService
- mcpTools -> memory [H]: QBrainAiMcpTools ctor: IMemoryService, ITransactionGatedMemoryService
- mcpTools -> search [H]: QBrainAiMcpTools ctor: IContextSearchService, IngestionCoordinator, IRepoFileService
- mcpTools -> triage [H]: QBrainAiMcpTools ctor: ITriageService, IHandoffIngestionService, IHostileReviewService
- mcpTools -> workspace [H]: QBrainAiMcpTools ctor: IWorkspaceService, WorkspaceServiceAccessor, WorkspaceContext
- mcpTools -> txsec [H]: QBrainAiMcpTools ctor: ITransactionGatedTodoMutationService, ITurnTransactionCoordinator
- mcpTools -> graphrag [H]: QBrainAiMcpTools ctor: IGraphRagService
- mcpTools -> github [H]: QBrainAiMcpTools ctor: IGitHubCliService
- mcpTools -> agents [H]: QBrainAiMcpTools ctor: DesktopLaunchService
- mcpTools -> tools [H]: QBrainAiMcpTools ctor: IPromptTemplateService
- mcpTools -> storage [H]: QBrainAiMcpTools ctor: McpDbContext, ISyncStatusStore
- api -> workspace [H]: WorkspaceController, WorkspaceValidationController ctors
- api -> txsec [H]: TodoController (ITransactionGatedTodoMutationService), TurnTransactionsController (ITurnTransactionCoordinator, ITransactionPubSubReplayService)
- api -> todo [H]: TodoController, TodoExecutionController ctors
- api -> sessionLog [H]: SessionLogController, SessionLogTranscriptIngestionController ctors
- api -> requirements [H]: RequirementsController, RequirementsRecoveryController, UseCases controllers
- api -> memory [H]: MemoryController ctor: IMemoryService
- api -> search [H]: ContextController, RepoController ctors
- api -> graphrag [H]: GraphRagController, ContextController ctors: IGraphRagService
- api -> triage [H]: TriageController, HandoffController, HostileReviewController ctors
- api -> brain [H]: BrainSlotsController, QuadBrainOpenAiController ctors
- api -> agents [H]: AgentPoolController, DesktopController ctors
- api -> federation [H]: FederationController ctor
- api -> tunnel [H]: TunnelController ctor: TunnelRegistry
- api -> github [H]: GitHubController ctor
- api -> tools [H]: ToolRegistryController, PromptTemplateController ctors
- api -> pairing [H]: QuadBrainOpenAiController ctor: WorkspaceTokenService (the /pair pages are minimal APIs in Program.cs, not controllers)
- api -> storage [H]: ContextController (McpDbContext), TriageController (McpDatabaseRuntimeOptions)
- pairing -> tools [H]: PairingHtmlRenderer ctor: IPromptTemplateService
- txsec -> todo [H]: TransactionGatedTodoMutationService ctor (TodoCreationService, TodoUpdateService, TodoServiceResolver)
- txsec -> sessionLog [H]: TransactionGatedSessionLogService ctor
- txsec -> memory [H]: TransactionGatedMemoryService ctor
- txsec -> requirements [H]: TransactionGatedRequirementsDocumentService and AnalysisService ctors
- txsec -> graphrag [H]: TransactionGatedGraphRagService ctor
- txsec -> workspace [H]: TransactionGatedTodoMutationService ctor (IWorkspaceService, WorkspaceContext)
- txsec -> tools [H]: TransactionGatedToolRegistryService, ToolBucketService, PromptTemplateService ctors
- txsec -> github [H]: TransactionGatedGitHubCliService, WorkspaceTokenStore, IssueTodoSyncService ctors
- txsec -> search [H]: TransactionGatedRepoFileService ctor
- txsec -> agents [H]: TransactionGatedAgentPoolService ctor
- txsec -> storage [H]: TransactionGated* ctors (McpDbContext)
- txsec -> qbrain.subscriber [M]: TransactionSecurityServiceCollectionExtensions.cs (AddTransactionPubSub, HttpSubscriberTransactionPubSub); default PubSubTransport=Direct in TransactionSecurityOptions.cs; Program.cs:214 AddInProcessTransactionSecurity
- todo -> github [H]: TodoUpdateService (IIssueTodoSyncService), TodoCreationService (IGitHubCliService)
- todo -> workspace [H]: TodoUpdateService, TodoPromptService, TodoExecutionService
- todo -> requirements [H]: TodoExecutionService ctor: IRequirementsDocumentService
- todo -> federation [H]: FederatedTodoService ctor (FederationRegistry, IFederationDataClient); Program.cs:633
- todo -> tools [H]: TodoPromptProvider ctor: IPromptTemplateService
- todo -> repoFs [H]: TodoBootstrapImporter hosted service (Program.cs:722)
- todo -> storage [H]: EfTodoService, TodoExecutionService (McpDbContext)
- sessionLog -> storage [H]: SessionLogService ctor (McpDbContext)
- sessionLog -> workspace [H]: SessionLogService ctor (WorkspaceContext)
- sessionLog -> federation [H]: FederatedSessionLogService ctor; Program.cs:649
- sessionLog -> repoFs [H]: SessionLogFileWatcher hosted service (Program.cs:705)
- requirements -> storage [H]: Requirements and UseCase queries take McpDbContext
- requirements -> workspace [H]: Requirements and UseCase queries take WorkspaceContext
- memory -> storage [H]: MemoryService and operations take McpDbContext
- graphrag -> storage [H]: GraphRagService ctor (McpDbContext)
- graphrag -> search [H]: GraphRagService ctor (IContextSearchService, IEmbeddingService, IVectorIndexService)
- graphrag -> federation [H]: FederatedGraphRagService ctor
- graphrag -> workspace [H]: GraphRagService ctor (WorkspaceContext)
- search -> storage [H]: HybridSearchService, Fts5SearchService, IngestionCoordinator ctors
- search -> workspace [H]: RepoFileService, RepoIngestor, WebsiteIngestor ctors (WorkspaceContext)
- search -> github [H]: IssueIngestor, GitHubIngestor ctors (IGitHubCliService)
- search -> sessionLog [H]: IngestionCoordinator ctor (SessionLogIngestor)
- search -> hf [H]: EmbeddingService.cs:18-19
- search -> qbrain.vector [H]: VectorIndexService, VectorIndexStartupService (Program.cs:706)
- search -> qbrain.repoFs [H]: RepoIngestor, RepoFileService
- triage -> storage [H]: TriageService, HostileReviewService, HandoffSourceResolver ctors
- triage -> workspace [H]: TriageService, HandoffIngestionService ctors
- triage -> todo [H]: TriageService ctor (ITodoService), ITriageTodoCreator
- triage -> agents [H]: ConfiguredTriageResearchRunner (IAgentExecutionStrategyResolver), HandoffOneShotExtractor (IAgentPoolService)
- triage -> tools [H]: TriageService ctor (IPromptTemplateService)
- triage -> sessionLog [H]: HandoffIngestionService ctor (ISessionLogSanitizer)
- brain -> txsec [H]: QuadBrainOrchestrationService (ITurnTransactionCoordinator), BrainSlotRegistryService (IKeyServerPartyRegistry)
- brain -> search [H]: BrainSlotContextAdmissionService ctor; QuadBrainInternalToolExecutor (IRepoFileService)
- brain -> sessionLog [H]: BrainInteractionSessionLogger, QuadBrainOpenAiChatService ctors
- brain -> todo [H]: QuadBrainInternalToolExecutor ctor
- brain -> requirements [H]: QuadBrainInternalToolExecutor ctor
- brain -> graphrag [H]: QuadBrainInternalToolExecutor ctor
- brain -> agents [H]: QuadBrainInternalToolExecutor ctor (IDesktopLaunchService)
- brain -> workspace [H]: QuadBrainOrchestrationService ctor
- brain -> storage [H]: BrainSlot* and QuadBrainOrchestrationService ctors
- brain -> llm [H]: CliBrainSlotChatClient.cs, BrainSlotChatClientFactory.cs
- agents -> tools [H]: AgentPoolService ctor (IPromptTemplateService, PromptTemplateRenderer)
- agents -> todo [H]: AgentPoolService ctor (ITodoPromptProvider)
- agents -> workspace [H]: AgentPoolService ctor (WorkspaceServiceAccessor)
- agents -> pairing [H]: HostedMcpAgentExecutionStrategy ctor (WorkspaceTokenService)
- agents -> storage [H]: AgentService ctor (McpDbContext)
- agents -> llm [H]: Codex/Copilot/Grok CliAgentExecutionStrategy.cs
- agents -> qbrain.launcher [M]: DesktopProcessSpawner registered at Program.cs:469; launcher exe invocation not traced to command line
- federation -> txsec [H]: TurnTransactionFederationOperationApplyService ctor (ITurnTransactionCoordinator)
- federation -> todo [H]: FederationPushService ctor
- federation -> sessionLog [H]: FederationPushService ctor
- federation -> storage [M]: Federation*Entity classes in Storage; ctor dependency not individually traced
- federation -> peer [H]: FederationProxyService HttpClient (Program.cs:600), FederationFanoutSyncService (712)
- tunnel -> workspace [H]: FrpTunnelProvider ctor (IWorkspaceProcessManager)
- tunnel -> tunnels [H]: Ngrok/Cloudflare/FrpTunnelProvider
- tunnel -> qbrain.frps [H]: FrpTunnelProvider; infra/frp/railway/README.md
- github -> workspace [H]: GitHubCliService, IssueTodoSyncService ctors (WorkspaceServiceAccessor)
- github -> github [H]: GitHubCliService.cs:17
- workspace -> storage [H]: WorkspaceService, WorkspacePolicyService ctors
- workspace -> pairing [H]: WorkspaceProcessManager ctor (WorkspaceTokenService)
- workspace -> todo [H]: WorkspaceServiceAccessor ctor (TodoServiceResolver)
- workspace -> sessionLog [H]: WorkspacePolicyService ctor (ISessionLogService)
- workspace -> federation [H]: WorkspaceProcessManager ctor (FederationRegistry)
- workspace -> qbrain.repoFs [H]: WorkspaceProjectionWriter, MarkerFileService
- storage -> workspace [H]: McpDbContext ctor (WorkspaceContext)
- storage -> qbrain.db [H]: McpDbContext, provider strategies
- tools -> storage [H]: Products queries take McpDbContext

## Step B: deployment evidence

Five deployment environments, each backed by a file in the repo. No environment was invented. Paths are repo-relative at commit 36783b79.

- DockerCompose (H): docker-compose.mcp.yml (service qbrain-ai, port 7147, volume qbrain-ai-data at /data, /workspace bind mount read-only from MCP_WORKSPACE, env Mcp__DataSource=mcp.db, VectorIndex__IndexPath=/data/vector.idx, /health healthcheck, restart unless-stopped) and Dockerfile (multi-stage, dotnet sdk 9.0 then aspnet 9.0, publishes QBrainAi.Support.Mcp, ENTRYPOINT dotnet QBrainAi.Support.Mcp.dll).
- WindowsService (H for server and key server, M for launcher placement): src/QBrainAi.Support.Mcp/Program.cs lines 86-129 (ResolveWindowsServiceName, default QBrainAi, UseWindowsService); scripts/Update-McpService.ps1 (ServiceName default QBrainAi, InstallPath default C:\ProgramData\QBrainAi, Port 7147, ExeName QBrainAi.Support.Mcp.exe); build/Build.UpdateService.cs; build/WindowsServiceHelper.cs; src/QBrainAi.Support.Mcp/Services/WindowsServiceDeploymentGuard.cs (requires .mcpservice-deployment.json and executable hashes); src/QBrainAi.KeyServer/Program.cs line 16-18 (service name QBrainAiKeyServer). Launcher on the interactive desktop is M: src/QBrainAi.Launcher uses CreateProcessWithTokenW; which session hosts it was not traced. The Windows database is not modeled because the repo does not fix it (SQLite default, SQL Server or PostgreSQL optional).
- LinuxSystemd (H, host-specific): docs/setup/install-local-service.ps1 creates mcpserver.service (ExecStart /opt/mcpserver/app/McpServer.Support.Mcp --urls http://localhost:7147, User mcpserver, EnvironmentFile /etc/mcpserver/mcpserver.env), a database McpServer_Omarchy and login on an existing sql-server-developer Docker container, and ACLs on a named workspace. It is written for PAYTON-OMARCHY, so it is an example, not a generic deployment. The script names the binary McpServer.Support.Mcp, which predates the QBrainAi rename (M that it still matches develop output).
- FrpRelay on Railway (H): infra/frp/railway/Dockerfile (alpine 3.20, frps 0.61.1), entrypoint.sh, frps.toml.template, README.md and .env.example (variables FRP_TOKEN, FRPS_BIND_PORT, FRPS_VHOST_HTTP_PORT, FRPS_ALLOW_PORTS_START/END; TCP proxy ports), docs/Operations/Tunnel-FRP-Railway.md. This is Railway for the relay only. No Railway deployment of the MCP server itself exists in the repo.
- FrpRelayLocal (H): infra/frp/local/docker-compose.frps.yml (ports 7000 and 8080).
- Not modeled because no evidence: Kubernetes, Helm, Azure or other cloud hosting of the server, a Railway MCP server service. src/QBrainAi.ServiceDefaults/RailwayConnectionStringBuilder.cs is a generic PostgreSQL connection helper, not a deployment of this system; it is not used by this system's hosts.
- Subscriber has no UseWindowsService call and no compose service in the repo, so it is not placed in any deployment view.

