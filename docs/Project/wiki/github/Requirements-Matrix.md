# Requirements Matrix (MCP Server)

Traceability policy: see `Requirements-Traceability-Policy.md`.

| Requirement | Status | Source Files |
| --- | --- | --- |
| FR-SUPPORT-010 | ✅ Complete | ContextController, TodoController, RepoController, SessionLogController, McpServerMcpTools, HybridSearchService, Fts5SearchService, VectorIndexService |
| FR-MCP-001 | ✅ Complete | IngestionOptions, IOptions |
| FR-MCP-002 | ✅ Complete | TodoController, TodoService, SqliteTodoService |
| FR-MCP-003 | ✅ Complete | SessionLogController, SessionLogService |
| FR-MCP-004 | ✅ Complete | HybridSearchService, Fts5SearchService, VectorIndexService |
| FR-MCP-005 | ✅ Complete | GitHubController, GitHubCliService, IssueTodoSyncService |
| FR-MCP-006 | ✅ Complete | IngestionCoordinator, RepoIngestor, SessionLogIngestor |
| FR-MCP-007 | ✅ Complete | Program.cs, McpServerMcpTools, McpStdioHost |
| FR-MCP-008 | ✅ Complete | Dockerfile, docker-compose.mcp.yml |
| FR-MCP-009 | ✅ Complete | WorkspaceController, WorkspaceService |
| FR-MCP-011 | ✅ Complete | WorkspaceProcessManager |
| FR-MCP-012 | ✅ Complete | ToolRegistryController, ToolRegistryService, ToolBucketService |
| FR-MCP-013 | ✅ Complete | WorkspaceAuthMiddleware, WorkspaceTokenService, MarkerFileService |
| FR-MCP-014 | ✅ Complete | PairingHtml, PairingOptions, Program.cs (/pair) |
| FR-MCP-015 | ✅ Complete | NgrokTunnelProvider, CloudflareTunnelProvider, FrpTunnelProvider |
| FR-MCP-016 | ✅ Complete | Program.cs (MapMcp), ModelContextProtocol.AspNetCore |
| FR-MCP-017 | ✅ Complete | Program.cs (UseWindowsService), Manage-McpService.ps1 |
| FR-MCP-018 | ✅ Complete | MarkerFileService, WorkspaceProcessManager |
| FR-MCP-019 | 🔀 Replaced | Replaced by FR-MCP-043 (single-app multi-tenant) |
| FR-MCP-020 | ✅ Complete | WorkspaceProcessManager (marker file writes) |
| FR-MCP-021 | ✅ Complete | WorkspaceController POST, WorkspaceService.InitAsync |
| FR-MCP-022 | ✅ Complete | ToolRegistryOptions, Program.cs (EnsureDefaultBucketsAsync) |
| FR-MCP-023 | ✅ Complete | RequirementsService, IRequirementsService, ICopilotClient |
| FR-MCP-024 | ✅ Complete | MarkdownSessionLogParser, SessionLogIngestor |
| FR-MCP-025 | ✅ Complete | WorkspaceProcessManager, WorkspaceConfigEntry, Program.cs |
| FR-LOC-001 | 🔲 Planned | — |
| TR-MCP-ARCH-001 | ✅ Complete | Core infrastructure |
| TR-MCP-DATA-001–003 | ✅ Complete | Storage and indexing |
| TR-MCP-CFG-001–002 | ✅ Complete | Configuration |
| TR-MCP-CFG-003 | ✅ Complete | WorkspaceConfigEntry schema + appsettings.json patch workflow |
| TR-MCP-INGEST-001–002 | ✅ Complete | Ingestion pipeline |
| TR-MCP-API-001 | ✅ Complete | REST API |
| TR-MCP-OPS-001 | ✅ Complete | Operational scripts |
| TR-MCP-WS-002–009 | ✅ Complete | Workspace management (TR-MCP-WS-006 obsolete) |
| TR-MCP-TR-001–003 | ✅ Complete | Tool registry |
| TR-MCP-SEC-001–002 | ✅ Complete | Security |
| TR-MCP-TUN-001–003 | ✅ Complete | Tunneling |
| TR-MCP-HTTP-001 | ✅ Complete | MCP transport |
| TR-MCP-SVC-001 | ✅ Complete | Windows service |
| TR-MCP-REQ-001 | ✅ Complete | AI requirements analysis |
| TR-MCP-REQ-002 | ✅ Complete | RequirementsDocumentService, RequirementsDocumentParser, RequirementsDocumentRenderer, RequirementsOptions |
| TR-MCP-REQ-003 | ✅ Complete | RequirementsController, FwhMcpTools, Program.cs (requirements DI/config) |
| TR-MCP-DRY-001 | ✅ Active directive | All code and scripts |
| TR-MCP-DOC-002 | ✅ Active directive | .github/copilot-instructions.md, AGENTS.md |
| FR-MCP-026 | ✅ Complete | OidcAuthOptions, Program.cs (JWT Bearer + AgentManager policy), WorkspaceAuthMiddleware, AgentController, AuthConfigController, Setup-McpKeycloak.ps1, setup-mcp-keycloak.sh, McpServerManager moved Director auth surfaces |
| FR-MCP-027 | ✅ Complete | Program.cs (startup built-in seeding), AgentController, AgentService, AgentDefaults, AgentDefinitionEntity |
| FR-MCP-028 | 🔲 Planned | AgentController, AgentService, AgentWorkspaceEntity, AgentEventLogEntity, McpDbContext |
| FR-MCP-029 | ✅ Complete | McpServer.Cqrs (Dispatcher, CallContext, CorrelationId, Result, IPipelineBehavior) |
| FR-MCP-030 | ✅ Complete | McpServerManager moved Director CLI/TUI surfaces |
| FR-MCP-031 | 🔲 Planned | — |
| FR-MCP-032 | 🔲 Planned | — |
| FR-MCP-033 | ✅ Complete | WorkspaceController (POST /mcpserver/workspace/policy), WorkspacePolicyService, WorkspacePolicyDirectiveParser, McpServerMcpTools.workspace_policy_apply |
| FR-MCP-034 | ✅ Complete | IWorkspaceService, MarkerFileService, WorkspaceModels |
| FR-MCP-035 | ✅ Complete | templates/prompt-templates.yaml |
| FR-MCP-036 | ✅ Complete | AuditedCopilotClient, Program.cs (ICopilotClient decorator), McpStdioHost (ICopilotClient decorator), CopilotServiceCollectionExtensions |
| FR-MCP-037 | ✅ Complete | McpServerManager moved Director exec/list-viewmodels surfaces, McpServer.Cqrs.Mvvm (IViewModelRegistry) |
| FR-MCP-038 | ✅ Complete | templates/prompt-templates.yaml |
| FR-MCP-039 | ✅ Complete | Program.cs + McpStdioHost PostConfigure<IngestionOptions>, appsettings.yaml RepoAllowlist, templates/prompt-templates.yaml |
| FR-MCP-040 | ✅ Complete | RequirementsController, RequirementsDocumentService, IRequirementsRepository |
| FR-MCP-041 | ✅ Complete | RequirementsController (/mcpserver/requirements/generate), RequirementsDocumentService, RequirementsDocumentRenderer |
| FR-MCP-042 | ✅ Complete | FwhMcpTools (requirements_* tools), RequirementsDocumentService |
| FR-MCP-043 | ✅ In Progress | WorkspaceResolutionMiddleware, WorkspaceContext, WorkspaceTokenService |
| FR-MCP-044 | ✅ In Progress | McpDbContext (global query filter), all entities (WorkspaceId) |
| TR-MCP-AUTH-001–003 | ✅ Complete | OidcAuthOptions, Program.cs (JwtBearer + AgentManager policy), WorkspaceAuthMiddleware, AgentController, Setup-McpKeycloak.ps1, setup-mcp-keycloak.sh, McpServerManager moved Director auth surfaces |
| TR-MCP-AGENT-001–003 | ✅ Complete | AgentDefinitionEntity, AgentWorkspaceEntity, AgentEventLogEntity, McpDbContext, AgentDefaults, AgentService, AgentController, Program.cs (startup seeding), WorkspaceAppFactory (primary-only controller exposure) |
| TR-MCP-CQRS-001–005 | ✅ Complete | McpServer.Cqrs (Dispatcher, CallContext, CorrelationId, Result, IPipelineBehavior, ILoggerProvider) |
| TR-MCP-DIR-001–003 | ✅ Complete | McpServerManager moved Director CLI/TUI surfaces |
| TR-MCP-COMP-001–003 | ✅ Complete | IWorkspaceService, MarkerFileService |
| TR-MCP-AUDIT-001 | ✅ Complete | AuditedCopilotClient, Program.cs decorator wiring, McpStdioHost decorator wiring |
| TR-MCP-POL-001 | ✅ Complete | WorkspacePolicyService, WorkspacePolicyDirectiveParser, WorkspaceController policy endpoint, McpServerMcpTools.workspace_policy_apply |
| TR-MCP-DTO-001 | ✅ Complete | UnifiedSessionLogDto |
| TR-MCP-CTX-001 | ✅ Complete | Program.cs + McpStdioHost PostConfigure<IngestionOptions>, appsettings.yaml RepoAllowlist, templates/prompt-templates.yaml |
| TR-MCP-MT-001 | ✅ Complete | WorkspaceContext, WorkspaceResolutionMiddleware |
| TR-MCP-MT-002 | ✅ Complete | WorkspaceResolutionMiddleware, WorkspaceTokenService |
| TR-MCP-MT-003 | ✅ Complete | McpDbContext (global query filter), all entities (WorkspaceId) |
| FR-MCP-045 | ✅ Complete | TodoController.MoveAsync, FwhMcpTools.TodoMove, TodoMoveRequest |
| FR-MCP-046 | ✅ Complete | VoiceController, VoiceConversationService, VoiceConversationOptions |
| FR-MCP-047 | ✅ Complete | DesktopProcessLauncher, NativeMethods, DesktopLaunchService, DesktopController, FwhMcpTools.DesktopLaunch |
| FR-MCP-048 | ✅ Complete | Program.cs (AddYamlFile), NetEscapades.Configuration.Yaml |
| TR-MCP-TODO-002 | ✅ Complete | TodoController, FwhMcpTools, TodoServiceResolver |
| TR-MCP-VOICE-001–003 | ✅ Complete | VoiceConversationService, VoiceController, VoiceConversationOptions |
| TR-MCP-CFG-004 | ✅ Complete | Program.cs, NetEscapades.Configuration.Yaml |
| TR-MCP-DESKTOP-001 | ✅ Complete | DesktopProcessLauncher, NativeMethods, DesktopLaunchService, DesktopController, FwhMcpTools.DesktopLaunch |
| FR-MCP-049 | ✅ Complete | PromptTemplateController, PromptTemplateService, PromptTemplateRenderer, TemplateClient, McpServerManager moved TemplatesScreen |
| TR-MCP-TPL-001 | ✅ Complete | PromptTemplateService, TemplateStorageOptions |
| TR-MCP-TPL-002 | ✅ Complete | PromptTemplateRenderer |
| TR-MCP-TPL-003 | ✅ Complete | PromptTemplateController, FwhMcpTools |
| TR-MCP-TPL-004 | ✅ Complete | TemplateMessages, \*TemplateQueryHandler, \*TemplateCommandHandler, TemplateApiClientAdapter, TemplateListViewModel, TemplateDetailViewModel, TemplatesScreen |
| FR-MCP-050 | ✅ Complete | IMarkerPromptProvider, FileMarkerPromptProvider, ITodoPromptProvider, TodoPromptProvider, PairingHtmlRenderer |
| TR-MCP-TPL-005 | ✅ Complete | IMarkerPromptProvider, FileMarkerPromptProvider, ITodoPromptProvider, TodoPromptProvider, PairingHtmlRenderer, templates/prompt-templates.yaml |
| FR-MCP-051 | 🔲 Planned | CopilotClientOptions, VoiceConversationOptions, AgentDefaults |
| TR-MCP-CFG-005 | 🔲 Planned | CopilotClientOptions, VoiceConversationOptions, AgentDefaults |
| FR-MCP-052 | ✅ Complete | AgentPoolOptions, AgentPoolDefinitionOptions, AgentPoolOptionsValidator, Program.cs (AgentPool registration), IAgentPoolService, AgentPoolService |
| FR-MCP-053 | ✅ Complete | AgentPoolService (queue lifecycle/dispatch), AgentPoolController (queue endpoints), TodoController queue enqueue endpoints |
| FR-MCP-054 | ✅ Complete | AgentPoolController, AgentPoolService (notification and per-job stream fan-out) |
| FR-MCP-055 | ✅ Complete | AgentPoolService (intent/context routing and default agent resolution), AgentPoolModels |
| FR-MCP-056 | ✅ Complete | PromptTemplateController, PromptTemplateService, PromptTemplateRenderer, AgentPoolService.ResolvePromptAsync, AgentPoolController queue/resolve |
| FR-MCP-057 | ✅ Complete | AgentPoolClient, Client.Models.AgentPoolModels, McpServerClient.AgentPool, McpServerManager moved Agent Pool UI surfaces |
| FR-MCP-058 | ✅ Complete | AgentPoolController SSE endpoints, AgentPoolService stream subscriptions, VoiceConversationService agent-session reuse/one-shot guard, VoiceController |
| TR-MCP-AGENT-004 | ✅ Complete | AgentPoolOptions, AgentPoolDefinitionOptions, AgentPoolOptionsValidator, Program.cs options validation/DI |
| TR-MCP-AGENT-005 | ✅ Complete | IAgentPoolService, AgentPoolService, AgentPoolController |
| TR-MCP-API-002 | ✅ Complete | AgentPoolController lifecycle/queue/resolve endpoints, AgentPoolService prompt/context routing |
| TR-MCP-API-003 | ✅ Complete | AgentPoolController notifications/jobs SSE, AgentPoolService notification + job stream channels |
| TR-MCP-TPL-006 | ✅ Complete | PromptTemplateController, PromptTemplateRenderer, AgentPoolService template/context prompt resolution |
| TR-MCP-VOICE-004 | ✅ Complete | VoiceConversationService pooled agent reuse + one-shot guard, AgentPoolService voice-runtime dispatch integration |
| TR-MCP-DIR-004 | ✅ Complete | AgentPoolClient, McpServerManager moved Agent Pool tab integration |
| FR-MCP-059 | 🔲 Planned | McpServer.Support.Mcp services/registries/managers/providers (DI SSOT state flow) |
| FR-MCP-060 | ✅ Complete | McpServerManager moved Core UI and Director surfaces, McpServer.Client adapters |
| FR-MCP-061 | ✅ Complete | TodoValidator, TodoService, SqliteTodoService, TodoCreationService, SessionLogIdentifierValidator, SessionLogController, SessionLogService |
| TR-MCP-DIR-005–008 | ✅ Complete | Endpoint-to-handler parity, ViewModel conventions, RBAC visibility/action mapping, declarative tab registry |
| TR-MCP-ARCH-002 | 🔲 Planned | DI lifetimes for state ownership, pull-notify flow via INotifyPropertyChanged, ActivatorUtilities remediation audit |
| TR-MCP-LOG-001 | ✅ Complete | Exception logging policy enforced across catch blocks (LogError/LogWarning) |
| TR-MCP-LOG-002 | ✅ Complete | TodoValidator, TodoService, SqliteTodoService, SessionLogIdentifierValidator, SessionLogController, SessionLogService |
| TEST-MCP-074 | ✅ Complete | TodoServiceTests, SqliteTodoServiceTests, SessionLogControllerTests, SessionLogServiceTests, MarkerFileServiceTests |
| FR-MCP-062 | ✅ Complete | IChangeEventBus, ChannelChangeEventBus, EventStreamController, mutation services/controllers/workspace process manager |
| TR-MCP-EVT-001 | ✅ Complete | ChannelChangeEventBus, IChangeEventBus, Program.cs (singleton registration) |
| TR-MCP-EVT-002 | ✅ Complete | TodoService, SqliteTodoService, SessionLogService, RepoFileService, ToolRegistryService, ToolBucketService, WorkspaceService, AgentService, RequirementsDocumentService, IngestionCoordinator, WorkspaceProcessManager |
| TR-MCP-EVT-003 | ✅ Complete | EventStreamController |
| TR-MCP-EVT-004 | ✅ Complete | ChangeEvent, ChangeEventActions, ChangeEventCategories |
| TR-MCP-EVT-005 | ✅ Complete | ChangeEventCategories, mutation publishers across workspace domains |
| TEST-MCP-075 | ✅ Complete | ChannelChangeEventBusTests |
| TEST-MCP-076 | ✅ Complete | TodoServiceTests, SqliteTodoServiceTests, SessionLogServiceTests, RepoFileServiceTests |
| TEST-MCP-077 | ✅ Complete | EventPublishingServiceTests |
| TEST-MCP-078 | ✅ Complete | EventStreamIntegrationTests |
| TEST-MCP-079 | ✅ Complete | EventStreamIntegrationTests |
| TEST-MCP-080 | ✅ Complete | EventStreamIntegrationTests (positive + non-matching category filter paths verified) |
| FR-MCP-063 | ✅ Complete | GitHubIntegrationOptions, FileGitHubWorkspaceTokenStore, GitHubController, GitHubCliService, ProcessRunner, GitHubClient |
| TR-MCP-GH-001 | ✅ Complete | GitHubIntegrationOptions, Program.cs, McpStdioHost, GitHubController |
| TR-MCP-GH-002 | ✅ Complete | IGitHubWorkspaceTokenStore, FileGitHubWorkspaceTokenStore, GitHubController |
| TR-MCP-GH-003 | ✅ Complete | IProcessRunner, ProcessRunner, GitHubCliService |
| TR-MCP-GH-004 | ✅ Complete | IGitHubCliService, GitHubCliService, GitHubController, McpServer.Client GitHub models/client |
| TEST-MCP-081 | ✅ Complete | GitHubControllerTests.AuthTokenEndpoints_RoundTrip |
| TEST-MCP-082 | ✅ Complete | GitHubControllerTests.OAuthConfig_AndAuthorizeUrlBehavior |
| TEST-MCP-083 | ✅ Complete | GitHubCliServiceTests.ListIssuesAsync_WithStoredWorkspaceToken_UsesProcessRunRequestOverride, FileGitHubWorkspaceTokenStoreTests |
| TEST-MCP-084 | ✅ Complete | GitHubCliServiceTests workflow run tests, GitHubControllerTests.ListWorkflowRuns_ReturnsOk, GitHubClientTests workflow/auth tests |
| TEST-MCP-085 | ✅ Complete | WorkspaceControllerTests.ApplyPolicy_ValidDirective_UpdatesWorkspaceBanList, WorkspaceControllerTests.ApplyPolicy_InvalidDirective_ReturnsBadRequest, WorkspacePolicyServiceTests |
| TEST-MCP-086 | ✅ Complete | AuditedCopilotClientTests, WorkspacePolicyDirectiveParserTests |
| TEST-MCP-087 | ✅ Complete | IngestionAllowlistContractTests.MarkerPromptTemplate_ContainsAvailableCapabilitiesSection |
| FR-MCP-065 | ✅ Complete | ContextController (ingest-website), IngestionCoordinator.IngestWebsiteAsync, WebsiteIngestor, FwhMcpTools.context_ingest_website, ContextClient.IngestWebsiteAsync |
| TR-MCP-INGEST-003 | ✅ Complete | WebsiteIngestor, IngestionOptions website limits, Program/McpStdioHost HttpClient registration, IngestionCoordinator website path |
| TEST-MCP-088 | ✅ Complete | WebsiteIngestorTests, ContextControllerTests (ingest-website), McpTransportTests (context_ingest_website), ContextClientTests.IngestWebsiteAsync_PostsTypedRequest |
| FR-MCP-066 | ✅ Complete | `McpServer.McpAgent` (`ServiceCollectionExtensions`, `McpAgentOptions`, `Hosting/*`, `PowerShellSessions/*`, `SessionLog/*`, `Todo/*`), `McpServer.Client` (`McpServerClient`, `RepoClient`, `DesktopClient`), `McpServer.McpAgent.SampleHost` (`Program.cs`, `SampleHostPreviewFactory.cs`) |
| TR-MCP-AGENT-006 | ✅ Complete | `ServiceCollectionExtensions`, `McpAgentOptions`, `McpAgentOptionsValidator`, `IMcpHostedAgent`, `IMcpHostedAgentFactory`, `McpHostedAgent`, `McpHostedAgentRegistration` |
| TR-MCP-AGENT-007 | ✅ Complete | `SessionLogWorkflow`, `SessionLogWorkflowContext`, `SessionLogTurnContext`, `TodoWorkflow`, `IMcpHostedAgent.PowerShellSessions`, `IHostedPowerShellSessionManager`, `McpHostedAgentToolAdapter`, `HostedPowerShellSessionManager`, `HostedPowerShellSessionHost`, `PowerShellSessionCreateResult`, `PowerShellSessionCommandResult`, `PowerShellSessionCloseResult`, `McpServerClient`, `RepoClient`, `DesktopClient`, `McpSessionIdentifierFactory` |
| TEST-MCP-089 | ✅ Complete | `HostedAgentWorkflowIntegrationTests`, `McpHostedAgentAdapterTests`, `DesktopClientTests`, `DesktopControllerTests`, `SessionLogWorkflowTests`, `TodoWorkflowTests`, `ServiceCollectionExtensionsTests`, `PowerShellSessions_ExecuteInteractiveCommand_PreservesHostLocalSessionState` |
| FR-MCP-067 | 🔲 Planned | — |
| TR-MCP-HTTP-002 | ✅ Complete | Program.cs centralized ProblemDetails handling, SessionLogController error paths |
| TEST-MCP-090 | 🔲 Planned | — |
| FR-MCP-068 | ✅ Complete | ConfigurationController, AppSettingsFileService, Program.cs (JWT Bearer auth), WorkspaceController |
| TR-MCP-CFG-006 | ✅ Complete | ConfigurationController, AppSettingsFileService, Program.cs (JWT Bearer auth), WorkspaceController |
| TEST-MCP-091 | ✅ Complete | ConfigurationControllerTests, AppSettingsFileServiceTests, ConfigurationAuthorizationPolicyTests |
| FR-MCP-069 | ✅ Complete | TodoCreationService, GitHubCliService, TodoController, FwhMcpTools, VoiceConversationService |
| TR-MCP-TODO-003 | ✅ Complete | TodoCreationService, TodoValidator, TodoController, FwhMcpTools, VoiceConversationService, TodoService, SqliteTodoService |
| TR-MCP-GH-005 | ✅ Complete | WorkspaceServiceAccessor, GitHubCliService, ProcessRunRequest |
| TEST-MCP-092 | ✅ Complete | TodoCreationServiceTests, TodoControllerTests |
| TEST-MCP-093 | ✅ Complete | GitHubCliServiceTests |
| FR-MCP-070 | ✅ Complete | TodoUpdateService, IssueTodoSyncService, TodoController, FwhMcpTools, VoiceConversationService |
| TR-MCP-TODO-004 | ✅ Complete | TodoUpdateService, TodoController, FwhMcpTools, VoiceConversationService |
| TR-MCP-GH-006 | ✅ Complete | IssueTodoSyncService, GitHubCliService |
| TEST-MCP-094 | ✅ Complete | TodoUpdateServiceTests, IssueTodoSyncServiceTests |
| FR-MCP-071 | ✅ Complete | IssueTodoSyncService, TodoUpdateService, GitHubController, TodoController |
| TR-MCP-GH-007 | ✅ Complete | IssueTodoSyncService |
| TEST-MCP-095 | ✅ Complete | IssueTodoSyncServiceTests, IssueTodoGitHubRoundTripIntegrationTests |
| FR-MCP-072 | ✅ Complete | EfTodoService, TodoYamlFileSerializer, TodoController, TodoClient, McpServerMcpTools, TodoServiceFactory, TodoBootstrapImporter |
| TR-MCP-TODO-005 | ✅ Complete | EfTodoService, TodoItemEntity, TodoAuditHistoryEntity, TodoDocumentMetadataEntity, McpDbContext, TodoYamlFileSerializer, TodoServiceFactory, TodoStorageOptions, McpInstanceResolver, appsettings*.yaml |
| TR-MCP-TODO-006 | ✅ Complete | ITodoService, ITodoStore, EfTodoService, TodoController, McpServerMcpTools, TodoClient, TodoModels, TodoCreationService, TodoUpdateService |
| TEST-MCP-096 | ✅ Complete | EfTodoServiceTests, TodoBootstrapImporterTests, SqliteTodoServiceTests, MixedTodoStorageIsolationTests |
| TEST-MCP-097 | ✅ Complete | EfTodoServiceTests, SqliteTodoServiceTests, TodoControllerTests, TodoClientTests, IntegrationTests Controllers.TodoControllerTests |
| FR-MCP-073 | ✅ Complete | ParseableEventFormatter, ParseableBatchFormatter |
| TR-MCP-LOG-003 | ✅ Complete | ParseableEventFormatter, ParseableBatchFormatter |
| TEST-MCP-098 | ✅ Complete | ParseableEventFormatterTests |
| FR-MCP-074 | ✅ Complete | azure-pipelines.yml, docs/AZURE-PIPELINES.md |
| TR-MCP-CI-001 | ✅ Complete | azure-pipelines.yml, docs/AZURE-PIPELINES.md, README.md, docs/MCP-SERVER.md, docs/RELEASE-CHECKLIST.md |
| TEST-MCP-099 | ✅ Complete | azure-pipelines.yml, docs/AZURE-PIPELINES.md |
| FR-MCP-075 | ✅ Complete | tools/powershell/McpSession.psm1 |
| TR-MCP-AGENT-013 | ✅ Complete | tools/powershell/McpSession.psm1 |
| TEST-MCP-100 | ✅ Complete | tools/powershell/McpSession.Tests.ps1 |
| FR-MCP-076 | ✅ Complete | src/McpServer.Services/Services/MarkerFileService.cs, templates/prompt-templates.yaml, src/McpServer.ServiceDefaults/Extensions.cs, tools/powershell/McpSession.psm1, tools/powershell/McpTodo.psm1, tools/powershell/McpContext.psm1, docs/context/module-bootstrap.md, docs/USER-GUIDE.md |
| TR-MCP-SEC-003 | ✅ Complete | src/McpServer.Services/Services/MarkerFileService.cs, templates/prompt-templates.yaml, src/McpServer.ServiceDefaults/Extensions.cs, tools/powershell/McpSession.psm1, tools/powershell/McpTodo.psm1, tools/powershell/McpContext.psm1 |
| TR-MCP-AGENT-014 | ✅ Complete | tools/powershell/McpSession.psm1, tools/powershell/McpTodo.psm1, tools/powershell/McpContext.psm1, docs/context/module-bootstrap.md, docs/USER-GUIDE.md |
| TR-MCP-AGENT-015 | ✅ Complete | QBAgentDefinition, McpAgentOptions, McpHostedAgent, McpAcidHostedAgentRuntime, McpHostedAgentAdapterTests, ServiceCollectionExtensionsTests |
| TR-MCP-AGENT-016 | ✅ Complete | McpHostedAgentToolAdapter, McpQuadBrainCodingAgentRequest, QBAgentDefinition, McpHostedAgentAdapterTests, HostedAgentWorkflowIntegrationTests |
| TEST-MCP-101 | ✅ Complete | tests/McpServer.Support.Mcp.Tests/Services/MarkerFileServiceTests.cs, tests/McpServer.Support.Mcp.IntegrationTests/HealthEndpointTests.cs, tools/powershell/McpSession.Tests.ps1, tools/powershell/McpTodo.Tests.ps1 |
| FR-MCP-077 | ✅ In Progress | src/McpServer.Storage/Database/McpDatabaseProviderFactory.cs, src/McpServer.Storage/McpDbContextFactory.cs, src/McpServer.Storage/Database/McpDatabaseProviderKind.cs, src/McpServer.Storage/Database/McpDatabaseProviderOptions.cs, src/McpServer.Storage/Database/SqliteMcpDatabaseProviderStrategy.cs, src/McpServer.Storage/Database/PostgreSqlMcpDatabaseProviderStrategy.cs, src/McpServer.Storage/Database/SqlServerMcpDatabaseProviderStrategy.cs, src/McpServer.Support.Mcp/Options/McpDatabaseConfigurationResolver.cs, src/McpServer.Support.Mcp/Program.cs, src/McpServer.Support.Mcp/McpStdio/McpStdioHost.cs, src/McpServer.Support.Mcp/DatabaseMaintenance/McpDatabaseEncryptionTransitionCommand.cs, src/McpServer.Support.Mcp/DatabaseMaintenance/McpDatabaseEncryptionTransitionRunner.cs, scripts/Invoke-McpDatabaseEncryptionTransition.ps1, src/McpServer.Storage.SqliteMigrations, src/McpServer.Storage.PostgreSqlMigrations, src/McpServer.Storage.SqlServerMigrations, docs/USER-GUIDE.md |
| TR-MCP-SEC-004 | ✅ In Progress | src/McpServer.Storage/Database/McpDatabaseProviderFactory.cs, src/McpServer.Storage/McpDbContextFactory.cs, src/McpServer.Storage/Database/SqliteMcpDatabaseProviderStrategy.cs, src/McpServer.Storage/Database/PostgreSqlMcpDatabaseProviderStrategy.cs, src/McpServer.Storage/Database/SqlServerMcpDatabaseProviderStrategy.cs, src/McpServer.Support.Mcp/DatabaseMaintenance/McpDatabaseEncryptionTransitionCommand.cs, src/McpServer.Support.Mcp/DatabaseMaintenance/McpDatabaseEncryptionTransitionRunner.cs, scripts/Invoke-McpDatabaseEncryptionTransition.ps1, src/McpServer.Storage.SqliteMigrations, src/McpServer.Storage.PostgreSqlMigrations, src/McpServer.Storage.SqlServerMigrations |
| TR-MCP-CFG-007 | ✅ Complete | src/McpServer.Support.Mcp/Options/McpDatabaseConfigurationResolver.cs, src/McpServer.Storage/McpDbContextFactory.cs, src/McpServer.Support.Mcp/Program.cs, src/McpServer.Support.Mcp/McpStdio/McpStdioHost.cs, src/McpServer.Support.Mcp/appsettings.yaml, src/McpServer.Support.Mcp/appsettings.Staging.yaml |
| TEST-MCP-102 | ✅ In Progress | tests/McpServer.Support.Mcp.IntegrationTests/Controllers/ProviderDatabaseIntegrationTests.cs, tests/McpServer.Support.Mcp.IntegrationTests/ProviderIntegrationTestSupport.cs, tests/McpServer.Support.Mcp.Tests/DatabaseMaintenance/McpDatabaseEncryptionTransitionCommandTests.cs, src/McpServer.Support.Mcp/DatabaseMaintenance/McpDatabaseEncryptionTransitionCommand.cs, src/McpServer.Support.Mcp/DatabaseMaintenance/McpDatabaseEncryptionTransitionRunner.cs, scripts/Invoke-McpDatabaseEncryptionTransition.ps1, src/McpServer.Storage.SqliteMigrations, src/McpServer.Storage.PostgreSqlMigrations, src/McpServer.Storage.SqlServerMigrations |
| FR-MCP-REPL-001 | ✅ Complete | McpServer.Repl.Core (IReplProtocol, IYamlEnvelope, IYamlSerializer, IMarkerFileReader, ITrustBootstrapService, IAuthRotationHandler, IWorkspaceSelector), McpServer.Repl.Host (Program.cs, AgentStdioHandler, InteractiveHandler, ServiceCollectionExtensions) |
| FR-MCP-REPL-002 | ✅ Complete | McpServer.Repl.Host (Program.cs, AgentStdioHandler, InteractiveHandler), McpServer.Repl.Core (SessionLogErrorEnvelope) |
| FR-MCP-REPL-003 | ✅ Complete | McpServer.Repl.Core (ITodoWorkflow, TodoCommandShapes, ISessionLogWorkflow, SessionLogCommandShapes, SessionLogModels, IRequirementsWorkflow, RequirementsCommandShapes, RequirementsCommandModels, IGenericClientPassthrough, ClientCommandShapes), McpServer.Repl.Host (TodoWorkflow, RequirementsWorkflow, SessionLogWorkflow, GenericClientPassthrough) |
| FR-MCP-REPL-004 | ✅ Complete | McpServer.Repl.Core (ITrustBootstrapService, IMarkerFileReader, IAuthRotationHandler), McpServer.Repl.Host (AgentStdioHandler) |
| FR-MCP-REPL-005 | ✅ Complete | McpServer.Repl.Core (IGenericClientPassthrough, ClientCommandShapes), McpServer.Repl.Host (GenericClientPassthrough) |
| TR-MCP-REPL-001 | ✅ Complete | McpServer.Repl.Core (IYamlEnvelope, IYamlSerializer, IReplProtocol) |
| TR-MCP-REPL-002 | ✅ Complete | McpServer.Repl.Host (ServiceCollectionExtensions, Program.cs), McpServer.Repl.Core workflow interfaces |
| TR-MCP-REPL-003 | ✅ Complete | McpServer.Repl.Host (Program.cs, AgentStdioHandler, InteractiveHandler), McpServer.Repl.Core (SessionLogErrorEnvelope) |
| TR-MCP-REPL-004 | ✅ Complete | McpServer.Repl.Core (ITodoWorkflow, ISessionLogWorkflow, IRequirementsWorkflow, IGenericClientPassthrough), McpServer.Repl.Host (TodoWorkflow, SessionLogWorkflow, RequirementsWorkflow, GenericClientPassthrough) |
| TR-MCP-REPL-005 | ✅ Complete | McpServer.Repl.Core (TodoCommandShapes, SessionLogCommandShapes, RequirementsCommandShapes, ClientCommandShapes), McpServer.Repl.Host (TodoWorkflow, SessionLogWorkflow, RequirementsWorkflow, GenericClientPassthrough) |
| TR-MCP-REPL-006 | ✅ Complete | McpServer.Repl.Core (ITrustBootstrapService, IMarkerFileReader, IAuthRotationHandler), McpServer.Repl.Host (AgentStdioHandler) |
| TR-MCP-REPL-007 | ✅ Complete | McpServer.Repl.Core (IGenericClientPassthrough, ClientCommandShapes), McpServer.Repl.Host (GenericClientPassthrough) |
| FR-SUPPORT-011 | ✅ Complete | src/McpServer.Services/Services/SessionLogService.cs (StampWorkspaceId), src/McpServer.Storage/McpDbContext.cs (auto-stamp fallback) |
| FR-SUPPORT-012 | ✅ Complete | src/McpServer.Support.Mcp/Program.cs (InvalidModelStateResponseFactory), src/McpServer.Support.Mcp/Controllers/SessionLogController.cs (ValidationProblem) |
| TR-SUPPORT-LOG-010 | ✅ Complete | Technical-Requirements.md, Program.cs, SessionLogController |
| FR-SUPPORT-013 | ✅ Complete | src/McpServer.Support.Mcp/Controllers/SessionLogController.cs (GetByIdAsync, UpsertTurnAsync), src/McpServer.Services/Services/SessionLogService.cs (GetAsync, UpsertTurnAsync) |
| FR-MCP-REPL-007 | ✅ Complete | src/McpServer.Repl.Host/MarkerFileClientOptionsResolver.cs (TryResolveWithDiagnostics, FindMarkerFile out-param), src/McpServer.Repl.Host/Program.cs (--workspace-path, --marker-file), src/McpServer.Client/McpClientBase.cs (CredentialDiagnostic surfacing) |
| FR-MCP-REPL-008 | ✅ Complete | src/McpServer.Repl.Host/Program.cs (--agent option + forwarding), MarkerFileClientOptionsResolver.cs (TryResolveWithDiagnostics + agent param, AgentOverride, GetCurrentAgent + per-agent VerifiedMarkerCacheEntry), plugins/core (repl-invoke.sh, repl-invoke.ps1, repl-bridge.ts, repl-daemon.js, repl-persistent.sh — all call sites) |
| TR-MCP-MT-004 | ✅ Complete | src/McpServer.Services/Services/SessionLogService.cs |
| TR-PLANNED-CORE-014 | ✅ Complete | src/McpServer.Support.Mcp/Program.cs |
| TR-MCP-REPL-008 | ✅ Complete | src/McpServer.Repl.Host/MarkerFileClientOptionsResolver.cs |
| TR-MCP-REPL-009 | ✅ Complete | src/McpServer.Repl.Host/Program.cs, MarkerFileClientOptionsResolver.cs (agent propagation + cache keying), plugins/core/* (enforced --agent on every repl call) |
| TEST-MCP-REPL-001 | ✅ Complete | tests/McpServer.Repl.Core.Tests (Iteration1_IntegrationTests, YamlFramingTests), tests/McpServer.Repl.IntegrationTests (YamlEnvelopeShapeTests) |
| TEST-MCP-REPL-002 | ✅ Complete | tests/McpServer.Repl.Core.Tests (FakeYamlSerializerTests, YamlFramingTests) |
| TEST-MCP-REPL-003 | ✅ Complete | tests/McpServer.Repl.Core.Tests (ProtocolHandshakeTests), tests/McpServer.Repl.IntegrationTests (TrustBootstrapFlowTests) |
| TEST-MCP-REPL-004 | ✅ Complete | tests/McpServer.Repl.IntegrationTests (TrustBootstrapFlowTests), tests/McpServer.Repl.Core.Tests (MarkerFileTrustTests, MockTrustBootstrapServiceTests) |
| TEST-MCP-REPL-005 | ✅ Complete | tests/McpServer.Repl.Core.Tests (AuthRotationTests, StubAuthRotationHandlerTests), tests/McpServer.Repl.IntegrationTests (AuthKeyAndWorkspaceTests) |
| TEST-MCP-REPL-006 | ✅ Complete | tests/McpServer.Repl.Core.Tests (TodoWorkflowTests, TodoWorkflowTestExtensions), tests/McpServer.Repl.IntegrationTests (Iteration3IntegrationTests) |
| TEST-MCP-REPL-007 | ✅ Complete | tests/McpServer.Repl.Core.Tests (SessionLogWorkflowTests, SessionLogWorkflowIntegration2Tests, SessionLogWorkflowProductionTests), tests/McpServer.Repl.IntegrationTests (Iteration2IntegrationTests) |
| TEST-MCP-REPL-008 | ✅ Complete | tests/McpServer.Repl.Core.Tests (GenericClientPassthroughTests), tests/McpServer.Repl.IntegrationTests (Iteration5IntegrationTests) |
| TEST-MCP-REPL-009 | ✅ Complete | tests/McpServer.Repl.Core.Tests (RequirementsWorkflowTests), tests/McpServer.Repl.IntegrationTests (Iteration4IntegrationTests) |
| TEST-MCP-REPL-010 | ✅ Complete | tests/McpServer.Repl.Core.Tests (WorkspaceSelectionTests), tests/McpServer.Repl.IntegrationTests (AuthKeyAndWorkspaceTests) |
| TEST-MCP-REPL-011 | ✅ Complete | tests/McpServer.Repl.Core.Tests (GenericClientPassthroughTests), tests/McpServer.Repl.IntegrationTests (Iteration5IntegrationTests) |
| TEST-MCP-REPL-012 | ✅ Complete | tests/McpServer.Repl.Core.Tests (TodoWorkflowTests streaming event tests) |
| TEST-MCP-REPL-013 | ✅ Complete | tests/McpServer.Repl.IntegrationTests (EndToEndFlowTests) |
| TEST-MCP-REPL-014 | ✅ Complete | tests/McpServer.Repl.Core.Tests (SessionLogWorkflowTests, TodoWorkflowTests error handling) |
| TEST-MCP-REPL-015 | ✅ Complete | tests/McpServer.Repl.Core.Tests (RequestResponseCorrelationTests), tests/McpServer.Repl.IntegrationTests (YamlEnvelopeShapeTests) |
| TEST-MCP-REPL-016 | ✅ Complete | tests/McpServer.Repl.Core.Tests (McpServerClientIntegrationTests, DI registration tests) |
| TEST-MCP-REPL-017 | ✅ Complete | tests/McpServer.Repl.Core.Tests (WorkspaceSelectionTests), tests/McpServer.Repl.IntegrationTests (AuthKeyAndWorkspaceTests) |
| TEST-MCP-REPL-018 | ✅ Complete | tests/McpServer.Repl.Core.Tests (OrchestrationRulesTests), tests/McpServer.Repl.IntegrationTests (TrustBootstrapFlowTests) |
| TEST-MCP-REPL-019 | ✅ Complete | tests/McpServer.Repl.Core.Tests (TodoWorkflowTests, SessionLogWorkflowTests, RequirementsWorkflowTests, GenericClientPassthroughTests) |
| TEST-MCP-REPL-020 | ✅ Complete | tests/McpServer.Repl.Core.Tests (SessionLogWorkflowTests state management, TodoWorkflowTests selection state) |
| FR-MCP-081 | ✅ Complete | TodoExecutionService, TodoExecutionModels, TodoExecutionController |
| FR-MCP-082 | ✅ Complete | TodoExecutionService, TodoExecutionController, McpServerMcpTools, TodoClient |
| FR-MCP-083 | ✅ Complete | TodoExecutionService, TodoExecutionController, McpServerMcpTools, TodoClient |
| TR-MCP-BYRD-001 | ✅ Complete | TodoExecutionModels, ITodoExecutionService, TodoExecutionService |
| TR-MCP-BYRD-002 | ✅ Complete | TodoExecutionService, TodoExecutionController, McpServerMcpTools, TodoClient |
| TR-MCP-BYRD-003 | ✅ Complete | TodoExecutionService, TodoExecutionController |
| TR-MCP-BYRD-004 | ✅ Complete | TodoExecutionController, McpServerMcpTools, TodoClient |
| TEST-MCP-103 | ✅ Complete | tests/McpServer.Support.Mcp.Tests/Services/TodoExecutionServiceTests.cs |
| TEST-MCP-104 | ✅ Complete | tests/McpServer.Support.Mcp.Tests/Services/TodoExecutionServiceTests.cs |
| TEST-MCP-105 | ✅ Complete | tests/McpServer.Support.Mcp.Tests/Controllers/TodoExecutionControllerTests.cs, tests/McpServer.Support.Mcp.Tests/McpStdio/TodoExecutionMcpToolTests.cs, tests/McpServer.Client.Tests/TodoClientTests.cs |
| FR-MCP-084 | ✅ Complete | RequirementsWikiDocumentRenderer, RequirementsWikiDocumentSelector, RequirementsController, RequirementsClient, RequirementsWorkflow, ReplCommandDispatcher, McpServerMcpTools, Codex/Claude/Copilot/Cline plugins |
| TR-MCP-REQ-004 | ✅ Complete | RequirementsWikiDocumentRenderer, RequirementsDocumentService, RequirementsDatabaseDocumentService, RequirementsController, RequirementsClient, RequirementsWorkflow, McpServerMcpTools |
| TR-MCP-REQ-005 | ✅ Complete | RequirementsWikiDocumentSelector, RequirementsController, RequirementsIngestRequest, RequirementsIngestResult, RequirementsClient, ReplCommandDispatcher, RequirementsWorkflow |
| TEST-MCP-106 | ✅ Complete | RequirementsDocumentServiceTests, RequirementsControllerTests |
| TEST-MCP-107 | ✅ Complete | RequirementsControllerTests |
| TEST-MCP-108 | ✅ Complete | RequirementsWorkflow, ReplCommandDispatcher, mcpserver-codex-plugin tests/repl-invoke-shim.bats |
| TEST-MCP-109 | ✅ Complete | mcpserver-codex-plugin tests/repl-invoke-shim.bats, Claude Code skills.bats, Copilot requirements skill, Cline requirements.test.ts |
| FR-MCP-078 | ✅ Complete | GraphRagController, GraphRagAdHocService, McpServer.GraphRag |
| FR-MCP-079 | ✅ Complete | GraphRagController, GraphRagAdHocService |
| FR-MCP-080 | ✅ Complete | GraphRagController, GraphRagAdHocService |
| TR-GRAPHRAG-ADHOC-001 | ✅ Complete | src/McpServer.GraphRag, src/McpServer.Support.Mcp/Controllers/GraphRagController.cs |
| TR-GRAPHRAG-ADHOC-002 | ✅ Complete | src/McpServer.GraphRag |
| TR-GRAPHRAG-ADHOC-003 | ✅ Complete | src/McpServer.GraphRag |
| TR-MCP-DOC-001 | ✅ Complete | docs/ folder structure, docs/MCP-SERVER.md, docs/USER-GUIDE.md, tests/Build.Tests/DocumentationGuidanceTests.cs |
| TR-MCP-TODO-007 | ✅ Complete | src/McpServer.Support.Mcp/Services/TodoCreationService.cs |
| TR-MCP-TODO-008 | ✅ Complete | src/McpServer.Storage/McpDbContext.cs, EfTodoService, TodoBootstrapImporter, AddTodoWorkspaceScoping migrations |
| TEST-MCP-001 | ✅ Complete | tests/McpServer.Support.Mcp.Tests/Configuration |
| TEST-MCP-002 | ✅ Complete | tests/McpServer.Support.Mcp.IntegrationTests/Controllers/TodoControllerTests.cs |
| TEST-MCP-003 | ✅ Complete | tests/McpServer.Support.Mcp.IntegrationTests (workspace isolation), src/McpServer.Storage/McpDbContext.cs (HasQueryFilter) |
| TEST-MCP-004 | ✅ Complete | tests/McpServer.Support.Mcp.Tests/Services/HybridSearchServiceTests.cs |
| TEST-MCP-005 | ✅ Complete | tests/McpServer.Support.Mcp.Tests/Services/IssueTodoSyncServiceTests.cs |
| TEST-MCP-006 | ✅ Complete | tests/McpServer.Support.Mcp.IntegrationTests/McpTransportTests.cs |
| TEST-MCP-007 | ✅ Complete | tests/McpServer.Support.Mcp.Tests/Services/WorkspaceServiceTests.cs |
| TEST-MCP-008 | ✅ Complete | tests/McpServer.Support.Mcp.Tests/Services/ToolRegistryServiceTests.cs |
| TEST-MCP-009 | ✅ Complete | tests/McpServer.Support.Mcp.Tests/Middleware/WorkspaceAuthMiddlewareTests.cs |
| TEST-MCP-010 | ✅ Complete | tests/McpServer.Support.Mcp.Tests/Services/PairingServiceTests.cs |
| TEST-MCP-011 | ✅ Complete | tests/McpServer.Support.Mcp.Tests/Services/TunnelProviderTests.cs |
| TEST-MCP-012 | ✅ Complete | tests/McpServer.Support.Mcp.IntegrationTests/McpTransportTests.cs |
| TEST-MCP-013 | ✅ Complete | tests/McpServer.Support.Mcp.Tests/Services/MarkerFileServiceTests.cs |
| TEST-MCP-014 | ✅ Complete | tests/McpServer.Support.Mcp.Tests/Services/RequirementsServiceTests.cs |
| TEST-MCP-015 | ✅ Complete | tests/McpServer.Support.Mcp.Tests/Services/MarkdownSessionLogParserTests.cs |
| TEST-MCP-026 | ✅ Complete | tests/McpServer.Support.Mcp.Tests |
| TEST-MCP-027 | ✅ Complete | tests/McpServer.Support.Mcp.Tests |
| TEST-MCP-028 | ✅ Complete | tests/McpServer.Support.Mcp.Tests |
| TEST-MCP-029 | ✅ Complete | tests/McpServer.Support.Mcp.Tests |
| TEST-MCP-030 | ✅ Complete | tests/McpServer.Support.Mcp.Tests |
| TEST-MCP-031 | ✅ Complete | tests/McpServer.Support.Mcp.Tests |
| TEST-MCP-032 | ✅ Complete | tests/McpServer.Support.Mcp.Tests |
| TEST-MCP-033 | ✅ Complete | tests/McpServer.Support.Mcp.Tests |
| TEST-MCP-034 | ✅ Complete | tests/McpServer.Support.Mcp.Tests |
| TEST-MCP-035 | ✅ Complete | tests/McpServer.Support.Mcp.Tests |
| TEST-MCP-036 | ✅ Complete | tests/McpServer.Support.Mcp.Tests |
| TEST-MCP-037 | ✅ Complete | tests/McpServer.Support.Mcp.Tests |
| TEST-MCP-038 | ✅ Complete | tests/McpServer.Support.Mcp.Tests |
| TEST-MCP-039 | ✅ Complete | tests/McpServer.Support.Mcp.Tests |
| TEST-MCP-040 | ✅ Complete | tests/McpServer.Support.Mcp.Tests |
| TEST-MCP-041 | ✅ Complete | tests/McpServer.Support.Mcp.Tests |
| TEST-MCP-042 | ✅ Complete | tests/McpServer.Support.Mcp.Tests |
| TEST-MCP-043 | ✅ Complete | tests/McpServer.Support.Mcp.Tests |
| TEST-MCP-044 | ✅ Complete | tests/McpServer.Support.Mcp.Tests |
| TEST-MCP-045 | ✅ Complete | tests/McpServer.Support.Mcp.Tests |
| TEST-MCP-046 | ✅ Complete | tests/McpServer.Support.Mcp.Tests |
| TEST-MCP-047 | ✅ Complete | tests/McpServer.Support.Mcp.Tests |
| TEST-MCP-048 | ✅ Complete | tests/McpServer.Support.Mcp.Tests |
| TEST-MCP-049 | ✅ Complete | tests/McpServer.Support.Mcp.Tests |
| TEST-MCP-050 | ✅ Complete | tests/McpServer.Support.Mcp.Tests |
| TEST-MCP-051 | ✅ Complete | tests/McpServer.Support.Mcp.Tests |
| TEST-MCP-052 | ✅ Complete | tests/McpServer.Support.Mcp.Tests |
| TEST-MCP-053 | ✅ Complete | tests/McpServer.Support.Mcp.Tests |
| TEST-MCP-054 | ✅ Complete | tests/McpServer.Support.Mcp.Tests |
| TEST-MCP-055 | ✅ Complete | tests/McpServer.Support.Mcp.Tests |
| TEST-MCP-056 | ✅ Complete | tests/McpServer.Support.Mcp.Tests |
| TEST-MCP-057 | ✅ Complete | tests/McpServer.Support.Mcp.Tests |
| TEST-MCP-058 | ✅ Complete | tests/McpServer.Support.Mcp.Tests |
| TEST-MCP-059 | ✅ Complete | tests/McpServer.Support.Mcp.Tests |
| TEST-MCP-060 | ✅ Complete | tests/McpServer.Support.Mcp.Tests |
| TEST-MCP-061 | ✅ Complete | tests/McpServer.Support.Mcp.Tests |
| TEST-MCP-062 | ✅ Complete | tests/McpServer.Support.Mcp.Tests |
| TEST-MCP-063 | ✅ Complete | tests/McpServer.Support.Mcp.Tests |
| TEST-MCP-064 | ✅ Complete | tests/McpServer.Support.Mcp.Tests |
| TEST-MCP-065 | ✅ Complete | tests/McpServer.Support.Mcp.Tests |
| TEST-MCP-066 | ✅ Complete | tests/McpServer.Support.Mcp.Tests |
| TEST-MCP-067 | ✅ Complete | tests/McpServer.Support.Mcp.Tests |
| TEST-MCP-068 | ✅ Complete | tests/McpServer.Support.Mcp.Tests |
| TEST-MCP-069 | ✅ Complete | tests/McpServer.Support.Mcp.Tests |
| TEST-MCP-070 | ✅ Complete | tests/McpServer.Support.Mcp.Tests |
| TEST-MCP-071 | ✅ Complete | tests/McpServer.Support.Mcp.Tests |
| TEST-MCP-072 | ✅ Complete | tests/McpServer.Support.Mcp.Tests |
| TEST-MCP-073 | ✅ Complete | tests/McpServer.Support.Mcp.Tests |
| FR-MCP-064 | 🔲 Planned | docs/ marketing pages (planned) |
| TR-MCP-AGENT-008 | 🔲 Planned | Reserved for FR-MCP-028 / FR-MCP-050 |
| TR-MCP-AGENT-009 | 🔲 Planned | Reserved for FR-MCP-050 |
| TR-MCP-AGENT-010 | 🔲 Planned | Reserved for FR-MCP-050 |
| TR-MCP-AGENT-011 | 🔲 Planned | Reserved for FR-MCP-050 |
| TR-MCP-AGENT-012 | 🔲 Planned | Reserved for FR-MCP-050 |
| TR-MCP-WS-UI-001 | 🔲 Planned | Reserved for FR-MCP-031 (Management Web UI) |
| FR-MCP-085 | 🔲 Planned | Question, service, controller, client, MCP, REPL, PowerShell Q&A implementation |
| FR-MCP-086 | 🔲 Planned | Answer, service, controller, client, MCP, REPL, PowerShell Q&A implementation |
| FR-MCP-087 | 🔲 Planned | Accepted-answer service and FAQ projection |
| FR-MCP-088 | 🔲 Planned | Question query and tag filtering |
| FR-MCP-089 | 🔲 Planned | QaVoteEntity, vote service, audit, voter endpoints |
| FR-MCP-090 | 🔲 Planned | Comment entity and service operations |
| FR-MCP-091 | 🔲 Planned | FAQ endpoint, client, wiki generator |
| FR-MCP-092 | 🔲 Planned | QaAuthorResolver |
| FR-MCP-093 | 🔲 Planned | Q&A EF workspace scoping |
| FR-MCP-094 | 🔲 Planned | QaController, MCP tools, QaClient |
| FR-MCP-095 | 🔲 Planned | QaWorkflow and McpQa PowerShell module |
| FR-MCP-096 | 🔲 Planned | Q&A docs and context reference |
| FR-MCP-097 | 🔲 Planned | Sibling plugin qa skills |
| FR-MCP-098 | 🔲 Planned | QaAuditHistoryEntity and audit endpoints |
| FR-MCP-099 | 🔲 Planned | Q&A skill web-research capture mandate |
| FR-MCP-100 | 🔲 Planned | Question close and duplicate flows |
| FR-MCP-101 | 🔲 Planned | QaBodyRenderer and body HTML fields |
| FR-MCP-102 | 🔲 Planned | FAQ wiki build target |
| TR-MCP-QA-001 | 🔲 Planned | Q&A entity tenancy |
| TR-MCP-QA-002 | 🔲 Planned | Question tags JSON |
| TR-MCP-QA-003 | 🔲 Planned | Provider migrations |
| TR-MCP-QA-004 | 🔲 Planned | Denormalized vote counters |
| TR-MCP-QA-005 | 🔲 Planned | Accepted answer storage |
| TR-MCP-QA-006 | 🔲 Planned | Q&A service shape |
| TR-MCP-QA-007 | 🔲 Planned | Q&A REST surface |
| TR-MCP-QA-008 | 🔲 Planned | Q&A search indexing |
| TR-MCP-QA-009 | 🔲 Planned | Q&A author resolver |
| TR-MCP-QA-010 | 🔲 Planned | Q&A MCP STDIO tools |
| TR-MCP-QA-011 | 🔲 Planned | FAQ query projection |
| TR-MCP-QA-012 | 🔲 Planned | Typed Q&A client |
| TR-MCP-QA-013 | 🔲 Planned | Q&A XML documentation |
| TR-MCP-QA-014 | 🔲 Planned | Q&A REPL workflow |
| TR-MCP-QA-015 | 🔲 Planned | Q&A PowerShell module |
| TR-MCP-QA-016 | 🔲 Planned | Plugin Q&A skill |
| TR-MCP-QA-017 | 🔲 Planned | Q&A documentation surface |
| TR-MCP-QA-018 | 🔲 Planned | Q&A audit storage |
| TR-MCP-QA-019 | 🔲 Planned | Q&A audit emission |
| TR-MCP-QA-020 | 🔲 Planned | Q&A audit query |
| TR-MCP-QA-021 | 🔲 Planned | Q&A audit surfaces |
| TR-MCP-QA-022 | 🔲 Planned | Answer sources JSON |
| TR-MCP-QA-023 | 🔲 Planned | Mandatory web capture skill rule |
| TR-MCP-QA-024 | 🔲 Planned | Companion web skill cross-references |
| TR-MCP-QA-025 | 🔲 Planned | Close and duplicate storage |
| TR-MCP-QA-026 | 🔲 Planned | Close and duplicate surfaces |
| TR-MCP-QA-027 | 🔲 Planned | Q&A body rendering |
| TR-MCP-QA-028 | 🔲 Planned | Q&A sanitization tests |
| TR-MCP-QA-029 | 🔲 Planned | FAQ wiki generation target |
| TR-MCP-QA-030 | 🔲 Planned | FAQ wiki snapshot tests |
| TR-MCP-QA-031 | 🔲 Planned | Q&A voter history |
| TR-MCP-QA-032 | 🔲 Planned | Q&A vote state storage |
| TR-MCP-QA-033 | 🔲 Planned | Q&A vote state machine |
| TR-MCP-QA-034 | 🔲 Planned | Q&A vote audit actions |
| TEST-MCP-110 | 🔲 Planned | Q&A question CRUD tests |
| TEST-MCP-111 | 🔲 Planned | Q&A answer CRUD tests |
| TEST-MCP-112 | 🔲 Planned | Accepted-answer invariant tests |
| TEST-MCP-113 | 🔲 Planned | Q&A tag filter tests |
| TEST-MCP-114 | 🔲 Planned | Q&A vote counter tests |
| TEST-MCP-115 | 🔲 Planned | Q&A comment tests |
| TEST-MCP-116 | 🔲 Planned | FAQ projection tests |
| TEST-MCP-117 | 🔲 Planned | Q&A search indexing tests |
| TEST-MCP-118 | 🔲 Planned | Q&A author resolver tests |
| TEST-MCP-119 | 🔲 Planned | Q&A workspace isolation tests |
| TEST-MCP-120 | 🔲 Planned | Q&A MCP STDIO parity tests |
| TEST-MCP-121 | 🔲 Planned | QaClient tests |
| TEST-MCP-122 | 🔲 Planned | QaWorkflow tests |
| TEST-MCP-123 | 🔲 Planned | McpQa PowerShell tests |
| TEST-MCP-124 | 🔲 Planned | Plugin skill smoke tests |
| TEST-MCP-125 | 🔲 Planned | Q&A audit emission tests |
| TEST-MCP-126 | 🔲 Planned | Q&A audit query tests |
| TEST-MCP-127 | 🔲 Planned | Q&A vote audit transaction tests |
| TEST-MCP-128 | 🔲 Planned | Answer sources tests |
| TEST-MCP-129 | 🔲 Planned | Q&A skill mandate text tests |
| TEST-MCP-130 | 🔲 Planned | Close and duplicate flow tests |
| TEST-MCP-131 | 🔲 Planned | Q&A sanitization tests |
| TEST-MCP-132 | 🔲 Planned | FAQ wiki generation tests |
| TEST-MCP-133 | 🔲 Planned | Voter-history endpoint tests |
| TEST-MCP-134 | 🔲 Planned | One-vote-per-user tests |
| TEST-MCP-135 | 🔲 Planned | Current vote state endpoint tests |
| FR-MCP-103 | 🟡 Partial | Hub-and-spoke federation |
| TR-MCP-FED-001 | 🟡 Partial | Hub proxy federation contract |
| TEST-MCP-136 | 🟡 Partial | Hub-and-spoke federation tests |
| FR-MCP-MEMORY-008 | 🟡 Partial | Federated memory state |
| TR-MCP-FED-MEMORY-001 | 🟡 Partial | Memory federation adapter contract |
| TEST-MCP-MEMORY-FED-001 | 🟡 Partial | Memory federation tests |
| FR-MCP-109 | ✅ Complete | Requirements batch mutation |
| TR-MCP-BATCH-109 | ✅ Complete | Requirements batch endpoint and workflow support |
| TR-MCP-SCHEMA-109 | ✅ Complete | REPL request schema enforcement |
| TR-MCP-STDIO-109 | ✅ Complete | Plugin stdio JSON request envelopes |
| TEST-MCP-145 | ✅ Complete | Requirements batch validation coverage |
| TEST-MCP-146 | ✅ Complete | Plugin stdio and schema validation coverage |
| TEST-MCP-147 | ✅ Complete | Documentation guidance contract tests |
| FR-MCP-110 | ✅ Complete | Deterministic Nuke PowerShell execution |
| TR-MCP-NUKE-001 | ✅ Complete | Non-interactive PowerShell hosts for Nuke automation |
| TEST-MCP-148 | ✅ Complete | Nuke PowerShell non-interactive guard tests |
| FR-MCP-104 | Tracked | Functional-Requirements.md |
| FR-MCP-105 | Tracked | Functional-Requirements.md |
| FR-MCP-106 | Tracked | Functional-Requirements.md |
| FR-MCP-107 | Tracked | Functional-Requirements.md |
| FR-MCP-108 | Tracked | Functional-Requirements.md |
| FR-WFL-001 | Tracked | Functional-Requirements.md |
| TR-MCP-BYRD-005 | Tracked | Technical-Requirements.md |
| TR-MCP-DB-001 | Tracked | Technical-Requirements.md |
| TR-MCP-DB-002 | Tracked | Technical-Requirements.md |
| TR-MCP-DB-003 | Tracked | Technical-Requirements.md |
| TR-MCP-DB-004 | Tracked | Technical-Requirements.md |
| TR-MCP-DB-005 | Tracked | Technical-Requirements.md |
| TR-MCP-PLAN-001 | Tracked | Technical-Requirements.md |
| TR-MCP-PLUGIN-008 | Tracked | Technical-Requirements.md |
| TR-MCP-TODO-009 | Tracked | Technical-Requirements.md |
| TR-MCP-TPL-007 | Tracked | Technical-Requirements.md |
| TR-MCP-WEB-001 | 🔲 Planned | Deferred to McpServerManager ownership boundary |
| TR-MCP-WEB-002 | 🔲 Planned | Deferred to McpServerManager ownership boundary |
| TR-MCP-WEB-003 | 🔲 Planned | Deferred to McpServerManager ownership boundary |
| TR-MCP-WEB-004 | 🔲 Planned | Deferred to McpServerManager ownership boundary |
| TR-TEST-INTEG-001 | Tracked | Technical-Requirements.md |
| TR-WFL-FULL-001 | Tracked | Technical-Requirements.md |
| TEST-GRAPHRAG-ADHOC-001 | Tracked | Testing-Requirements.md |
| TEST-GRAPHRAG-ADHOC-002 | Tracked | Testing-Requirements.md |
| TEST-GRAPHRAG-ADHOC-003 | Tracked | Testing-Requirements.md |
| TEST-GRAPHRAG-ADHOC-004 | Tracked | Testing-Requirements.md |
| TEST-GRAPHRAG-ADHOC-005 | Tracked | Testing-Requirements.md |
| TEST-GRAPHRAG-ADHOC-006 | Tracked | Testing-Requirements.md |
| TEST-GRAPHRAG-ADHOC-007 | Tracked | Testing-Requirements.md |
| TEST-MCP-137 | Tracked | Testing-Requirements.md |
| TEST-MCP-138 | Tracked | Testing-Requirements.md |
| TEST-MCP-139 | Tracked | Testing-Requirements.md |
| TEST-MCP-140 | Tracked | Testing-Requirements.md |
| TEST-MCP-141 | Tracked | Testing-Requirements.md |
| TEST-MCP-142 | Tracked | Testing-Requirements.md |
| TEST-MCP-143 | Tracked | Testing-Requirements.md |
| TEST-MCP-144 | Tracked | Testing-Requirements.md |
| TEST-MCP-REPL-021 | Tracked | Testing-Requirements.md |
| TEST-MCP-REPL-022 | Tracked | Testing-Requirements.md |
| TEST-MCP-REPL-023 | Tracked | Testing-Requirements.md |
| TEST-MCP-REPL-024 | Tracked | Testing-Requirements.md |
| TEST-SUPPORT-016 | Tracked | Testing-Requirements.md |
| TEST-SUPPORT-017 | Tracked | Testing-Requirements.md |
| TEST-SUPPORT-018 | Tracked | Testing-Requirements.md |
| TEST-SUPPORT-019 | Tracked | Testing-Requirements.md |
| TEST-SUPPORT-020 | Tracked | Testing-Requirements.md |
| TEST-SUPPORT-021 | Tracked | Testing-Requirements.md |
| TEST-SUPPORT-022 | Tracked | Testing-Requirements.md |
| TEST-SUPPORT-023 | ✅ Complete | SessionLogControllerTests, SessionLogServiceTests |
| TEST-WFL-001 | Tracked | Testing-Requirements.md |
| FR-GEN-001 | Tracked | Functional-Requirements.md |
| FR-TEST-002 | Tracked | Functional-Requirements.md |
| TR-GEN-YAML-001 | Tracked | Technical-Requirements.md |
| FR-TEST-001 | Tracked | Functional-Requirements.md |
| FR-MCP-111 | Tracked | Functional-Requirements.md |
| FR-MCP-112 | Tracked | Functional-Requirements.md |
| FR-MCP-113 | Tracked | Functional-Requirements.md |
| FR-MCP-114 | Tracked | Functional-Requirements.md |
| FR-MCP-115 | Tracked | Functional-Requirements.md |
| FR-MCP-AGENT-PARITY-001 | Tracked | Functional-Requirements.md |
| FR-MCP-AGENT-PARITY-002 | Tracked | Functional-Requirements.md |
| FR-MCP-BATCH-001 | Tracked | Functional-Requirements.md |
| FR-MCP-MEMORY-001 | ✅ Complete | Functional-Requirements.md |
| FR-MCP-MEMORY-002 | ✅ Complete | Functional-Requirements.md |
| FR-MCP-MEMORY-003 | ✅ Complete | Functional-Requirements.md |
| FR-MCP-MEMORY-004 | ✅ Complete | Functional-Requirements.md |
| FR-MCP-MEMORY-005 | ✅ Complete | Functional-Requirements.md |
| FR-MCP-MEMORY-006 | ✅ Complete | Functional-Requirements.md |
| FR-MCP-MEMORY-007 | ✅ Complete | Functional-Requirements.md |
| FR-MCP-PLUGIN-BATCH-001 | Tracked | Functional-Requirements.md |
| FR-MCP-PLUGIN-SKILLS-001 | Tracked | Functional-Requirements.md |
| FR-MCP-REQAC-001 | Tracked | Functional-Requirements.md |
| FR-MCP-REQAC-002 | Tracked | Functional-Requirements.md |
| FR-MCP-REQAC-PLUGIN-001 | Tracked | Functional-Requirements.md |
| FR-MCP-REQACPLUGIN-001 | Tracked | Functional-Requirements.md |
| FR-MCP-REQACPLUGIN-002 | Tracked | Functional-Requirements.md |
| TR-MCP-AGENT-PARITY-010 | Tracked | Technical-Requirements.md |
| TR-MCP-AGENT-PARITY-011 | Tracked | Technical-Requirements.md |
| TR-MCP-AGENT-PARITY-012 | Tracked | Technical-Requirements.md |
| TR-MCP-AGENT-PARITY-013 | Tracked | Technical-Requirements.md |
| TR-MCP-AGENT-PARITY-020 | Tracked | Technical-Requirements.md |
| TR-MCP-AGENT-PARITY-020..027 | Tracked | Technical-Requirements.md |
| TR-MCP-AGENT-PARITY-030 | Tracked | Technical-Requirements.md |
| TR-MCP-BATCH-001 | Tracked | Technical-Requirements.md |
| TR-MCP-BATCHTS-001 | Tracked | Technical-Requirements.md |
| TR-MCP-MEMORY-001 | ✅ Complete | Technical-Requirements.md |
| TR-MCP-MEMORY-002 | ✅ Complete | Technical-Requirements.md |
| TR-MCP-MEMORY-003 | ✅ Complete | Technical-Requirements.md |
| TR-MCP-MEMORY-004 | ✅ Complete | Technical-Requirements.md |
| TR-MCP-MEMORY-005 | ✅ Complete | Technical-Requirements.md |
| TR-MCP-MEMORY-006 | ✅ Complete | Technical-Requirements.md |
| TR-MCP-MEMORY-007 | ✅ Complete | Technical-Requirements.md |
| TR-MCP-MEMORY-008 | ✅ Complete | Technical-Requirements.md |
| TEST-MCP-MEMORY-001 | ✅ Complete | Testing-Requirements.md |
| TEST-MCP-MEMORY-002 | ✅ Complete | Testing-Requirements.md |
| TEST-MCP-MEMORY-003 | ✅ Complete | Testing-Requirements.md |
| TEST-MCP-MEMORY-004 | ✅ Complete | Testing-Requirements.md |
| TEST-MCP-MEMORY-005 | ✅ Complete | Testing-Requirements.md |
| TEST-MCP-MEMORY-006 | ✅ Complete | Testing-Requirements.md |
| TEST-MCP-MEMORY-007 | ✅ Complete | Testing-Requirements.md |
| TEST-MCP-MEMORY-008 | ✅ Complete | Testing-Requirements.md |
| TEST-MCP-MEMORY-009 | ✅ Complete | Testing-Requirements.md |
| TR-MCP-PLUGIN-009 | Tracked | Technical-Requirements.md |
| TR-MCP-PLUGIN-SKILLS-001 | Tracked | Technical-Requirements.md |
| TR-MCP-REQAC-001 | Tracked | Technical-Requirements.md |
| TR-MCP-REQAC-002 | Tracked | Technical-Requirements.md |
| TR-MCP-REQAC-PLUGIN-001 | Tracked | Technical-Requirements.md |
| TR-MCP-REQACPLUGIN-001 | Tracked | Technical-Requirements.md |
| TR-MCP-REQACPLUGIN-002 | Tracked | Technical-Requirements.md |
| TR-MCP-REQEXPORT-001 | Tracked | Technical-Requirements.md |
| TR-MCP-SKILLS-001 | Tracked | Technical-Requirements.md |
| TR-MCP-SKILLS-002 | Tracked | Technical-Requirements.md |
| TR-MCP-SKILLS-003 | Tracked | Technical-Requirements.md |
| TR-MCP-TODO-010 | Tracked | Technical-Requirements.md |
| TEST-MCP-149 | Tracked | Testing-Requirements.md |
| TEST-MCP-150 | Tracked | Testing-Requirements.md |
| TEST-MCP-151 | Tracked | Testing-Requirements.md |
| TEST-MCP-152 | Tracked | Testing-Requirements.md |
| TEST-MCP-153 | Tracked | Testing-Requirements.md |
| TEST-MCP-154 | Tracked | Testing-Requirements.md |
| TEST-MCP-155 | Tracked | Testing-Requirements.md |
| TEST-MCP-BATCH-001 | Tracked | Testing-Requirements.md |
| TEST-MCP-REQAC-001 | Tracked | Testing-Requirements.md |
| TEST-MCP-REQAC-002 | Tracked | Testing-Requirements.md |
| TEST-MCP-REQAC-003 | Tracked | Testing-Requirements.md |
| TEST-MCP-REQAC-004 | Tracked | Testing-Requirements.md |
| TEST-MCP-REQACPLUGIN-001 | Tracked | Testing-Requirements.md |
| TEST-MCP-REQACPLUGIN-002 | Tracked | Testing-Requirements.md |
| TEST-REQAC-LIVE-001 | Tracked | Testing-Requirements.md |
| FR-MCP-116 | Tracked | Functional-Requirements.md |
| FR-MCP-117 | Tracked | Functional-Requirements.md |
| FR-MCP-118 | ✅ Complete | KeyServerController, McpServer.KeyServer Program, KeyServerClient, HttpKeyServerManifestService, TransactionSecurityServices, TransactionSecurityOptions, TransactionSecurityServiceCollectionExtensions, TransactionSecurityStateStores, TransactionSecurityModels, TransactionSecurityControllerTests, TransactionSecurityClientTests, DurableTransactionSecurityStorageTests, SeparateTransactionServiceIntegrationTests |
| FR-MCP-119 | ✅ Complete | InMemorySubscriberCommitService, SubscriberController, SubscriberClient, McpServer.Subscriber Program, TransactionSecurityControllerTests, DurableTransactionSecurityStorageTests, DiffgramEncryptionIntegrationTests, SeparateTransactionServiceIntegrationTests |
| FR-MCP-120 | ✅ Complete | TransactionSecurityModels, TurnTransactionCoordinator, TransactionPubSubServices, TurnTransactionFederationOperationApplyService, TransactionGatedMemoryService, TransactionalTodoWorkflow, ITodoCompensationService, EfTodoService, FederatedTodoService, TransactionGatedTodoMutationService, TransactionGatedRepoFileService, TransactionGatedPromptTemplateService, TransactionGatedRequirementsDocumentService, TransactionGatedRequirementsAnalysisService, TransactionGatedTodoExecutionService, TransactionGatedSessionLogService, TransactionGatedToolRegistryService, TransactionGatedToolBucketService, TransactionGatedGraphRagService, TransactionGatedGitHubCliService, TransactionGatedGitHubWorkspaceTokenStore, TransactionGatedIssueTodoSyncService, TransactionGatedVoiceConversationService, TransactionGatedAgentPoolService, IClientMutationPolicy, KnownUnsafeClientMutationPolicy, GenericClientPassthrough, ReplCommandDispatcher, FederationController, MemoryController, TodoController, RepoController, PromptTemplateController, RequirementsController, SessionLogController, ToolRegistryController, GraphRagController, GitHubController, ContextController, VoiceController, AgentPoolController, McpServerMcpTools, FwhMcpTools.Todo, FwhMcpTools.GitHub, FwhMcpTools.Requirements, FwhMcpTools.SessionLog, FwhMcpTools.Context, McpStdioHost, Program.cs, ServiceCollectionExtensions.cs, TurnTransactions-Mutation-Endpoint-Audit.md, TurnTransactionCoordinatorTests, TransactionPubSubTests, ClientMutationPolicyTests, FederationControllerTests, FederationControllerPushTests, RequirementsControllerTransactionGateTests, ContextControllerTransactionGateTests, TransactionGatedVoiceConversationServiceTests, TransactionGatedAgentPoolServiceTests, VoiceControllerTests, TransactionGatedMemoryServiceTests, TransactionalTodoWorkflowTests, TransactionGatedTodoMutationServiceTests, TransactionGatedRepoFileServiceTests, TransactionGatedPromptTemplateServiceTests, TransactionGatedRequirementsDocumentServiceTests, TransactionGatedRequirementsAnalysisServiceTests, TransactionGatedTodoExecutionServiceTests, TransactionGatedSessionLogServiceTests, TransactionGatedToolRegistryServiceTests, TransactionGatedToolBucketServiceTests, TransactionGatedGraphRagServiceTests, TransactionGatedGitHubCliServiceTests, TransactionGatedGitHubWorkspaceTokenStoreTests, TransactionGatedIssueTodoSyncServiceTests, TransactionGatedStdioRoutingTests, GitHubControllerTests, MemoryControllerTests, MemoryMcpToolTests, TodoControllerTests, TodoExecutionMcpToolTests, GraphRagControllerAdHocTests, GraphRagMcpToolTests, SessionLogControllerTests, SessionLogReplaceDeleteControllerTests, ToolRegistryScopeTests, ToolBucketServiceTests, RepoFileServiceTests, PromptTemplateServiceTests, RequirementsDatabaseDocumentServiceTests, TodoExecutionServiceTests, EfTodoServiceTests, FederationOperationApplyServiceTests, SeparateTransactionServiceIntegrationTests, DurableTransactionSecurityStorageTests |
| FR-MCP-121 | ✅ Complete | TransactionSecurityModels, TransactionSecurityOptions, TransactionSecurityServiceCollectionExtensions, TurnTransactionCoordinator, TransactionPubSubServices, TransactionSecurityStateStores, TurnTransactionsController, TransactionPubSubReplayWorker, TurnTransactions-Mutation-Endpoint-Audit.md, TurnTransactionCoordinatorTests, TransactionPubSubTests, TurnTransactionsControllerTests, TransactionPubSubReplayWorkerTests, DurableTransactionSecurityStorageTests, SeparateTransactionServiceIntegrationTests, Functional-Requirements.md |
| FR-MCP-122 | ✅ Complete | Functional-Requirements.md, Quad-Model-Transactional-Diffgram-Plan.md, TurnTransactionPlanArtifactTests |
| FR-MCP-123 | ✅ Complete | Functional-Requirements.md, TurnTransactions-Architecture-Round1.md, TurnTransactions-Design-Round2.md, PlanTransactionReviewTests, TurnTransactionPlanArtifactTests |
| FR-MCP-124 | ✅ Complete | Functional-Requirements.md |
| FR-MCP-125 | ✅ Complete | Functional-Requirements.md, TurnTransactions-Design-Round2.md, Testing-Requirements.md, TurnTransactionPlanArtifactTests |
| FR-MCP-126 | ✅ Complete | Functional-Requirements.md, PlanTransactionReviewTests |
| FR-MCP-127 | ✅ Complete | Functional-Requirements.md, TurnTransactionPlanArtifactTests, SeparateTransactionServiceIntegrationTests, TurnTransactionCoordinatorTests |
| FR-MCP-128 | ✅ Complete | Functional-Requirements.md, Technical-Requirements.md, Testing-Requirements.md, Requirements-Matrix.md, TR-per-FR-Mapping.md, TurnTransactions-Mutation-Endpoint-Audit.md, TurnTransactionPlanArtifactTests |
| FR-MCP-129 | ✅ Complete | Functional-Requirements.md, BrainSlotRegistryServiceTests, BrainSlotsControllerTests, BrainSlotClientTests, BrainSlotCredentialResolverTests, BrainSlotInvocationTransactionTests, BrainSlotContractArtifactTests |
| FR-MCP-130 | ✅ Complete | Functional-Requirements.md, BrainSlotInvocationTransactionTests, BrainSlotContainmentTests |
| FR-MCP-131 | ✅ Complete | Functional-Requirements.md, QuadBrainOrchestrationServiceTests, BrainSlotContainmentTests, TurnTransactionPlanArtifactTests |
| FR-MCP-132 | ✅ Complete | Functional-Requirements.md, WorkspaceAuthMiddleware, WorkspaceTokenService, WorkspaceAuthMiddlewareTests, WorkspaceTokenServiceTests |
| FR-MCP-133 | ✅ Complete | Functional-Requirements.md, WorkspaceReadinessHealthCheck, WorkspaceReadinessHealthCheckTests, ReadinessAndAuthIntegrationTests |
| FR-MCP-134 | ✅ Complete | Functional-Requirements.md, QuadBrainOrchestrationService, QuadBrainOrchestrationServiceTests, BrainSlotsControllerTests, BrainSlotClientTests, BrainSlotContractArtifactTests |
| FR-MCP-135 | ✅ Complete | Functional-Requirements.md, BrainSlotDefinitionEntity, QuadBrainOrchestrationService, AddBrainSlotWeights migrations, QuadBrainOrchestrationServiceTests |
| FR-MCP-136 | ✅ Complete | Functional-Requirements.md, QBAgentDefinition, McpAgentOptions, McpHostedAgent, McpHostedAgentAdapterTests, ServiceCollectionExtensionsTests |
| FR-MCP-137 | ✅ Complete | Functional-Requirements.md, McpHostedAgentToolAdapter, McpHostedAgent, QBAgentDefinition, McpHostedAgentAdapterTests, HostedAgentWorkflowIntegrationTests |
| TR-MCP-GH-008 | Tracked | Technical-Requirements.md |
| TR-MCP-PLUGIN-010 | Tracked | Technical-Requirements.md |
| TR-MCP-KEYSERVER-001 | ✅ Complete | McpServer.KeyServer Program, KeyServerController, KeyServerClient, HttpKeyServerManifestService, TransactionSecurityServices, TransactionSecurityOptions, TransactionSecurityServiceCollectionExtensions, TransactionSecurityStateStores, TransactionSecurityModels, TransactionSecurityControllerTests, TransactionSecurityClientTests, DurableTransactionSecurityStorageTests, SeparateTransactionServiceIntegrationTests |
| TR-MCP-CRYPTO-001 | ✅ Complete | Technical-Requirements.md, TransactionSecurityModels, TransactionSecurityServices, TransactionSecurityControllerTests, TransactionSecurityClientTests, DurableTransactionSecurityStorageTests, DiffgramEncryptionIntegrationTests, SeparateTransactionServiceIntegrationTests |
| TR-MCP-SUBSCRIBER-001 | ✅ Complete | TransactionSecurityServices, TransactionSecurityStateStores, SubscriberController, SubscriberClient, McpServer.Subscriber Program, TransactionSecurityControllerTests, DiffgramEncryptionIntegrationTests, DurableTransactionSecurityStorageTests, SeparateTransactionServiceIntegrationTests |
| TR-MCP-TXN-001 | ✅ Complete | TransactionSecurityModels, TransactionSecurityOptions, TransactionSecurityServiceCollectionExtensions, TurnTransactionCoordinator, TransactionPubSubServices, TransactionSecurityStateStores, TurnTransactionFederationOperationApplyService, TransactionPubSubReplayWorker, TransactionGatedMemoryService, TransactionalTodoWorkflow, ITodoCompensationService, EfTodoService, FederatedTodoService, TransactionGatedTodoMutationService, TransactionGatedRepoFileService, TransactionGatedPromptTemplateService, TransactionGatedRequirementsDocumentService, TransactionGatedRequirementsAnalysisService, TransactionGatedTodoExecutionService, TransactionGatedSessionLogService, TransactionGatedToolRegistryService, TransactionGatedToolBucketService, TransactionGatedGraphRagService, TransactionGatedGitHubCliService, TransactionGatedGitHubWorkspaceTokenStore, TransactionGatedIssueTodoSyncService, TransactionGatedVoiceConversationService, TransactionGatedAgentPoolService, IClientMutationPolicy, KnownUnsafeClientMutationPolicy, GenericClientPassthrough, ReplCommandDispatcher, FederationController, TurnTransactionsController, MemoryController, TodoController, RepoController, PromptTemplateController, RequirementsController, SessionLogController, ToolRegistryController, GraphRagController, GitHubController, ContextController, VoiceController, AgentPoolController, McpServerMcpTools, FwhMcpTools.Todo, FwhMcpTools.GitHub, FwhMcpTools.Requirements, FwhMcpTools.SessionLog, FwhMcpTools.Context, McpStdioHost, Program.cs, ServiceCollectionExtensions.cs, TurnTransactions-Mutation-Endpoint-Audit.md, TurnTransactionCoordinatorTests, TransactionPubSubTests, TransactionPubSubReplayWorkerTests, ClientMutationPolicyTests, FederationControllerTests, FederationControllerPushTests, RequirementsControllerTransactionGateTests, ContextControllerTransactionGateTests, TransactionGatedVoiceConversationServiceTests, TransactionGatedAgentPoolServiceTests, VoiceControllerTests, TransactionGatedMemoryServiceTests, TransactionalTodoWorkflowTests, TransactionGatedTodoMutationServiceTests, TransactionGatedRepoFileServiceTests, TransactionGatedPromptTemplateServiceTests, TransactionGatedRequirementsDocumentServiceTests, TransactionGatedRequirementsAnalysisServiceTests, TransactionGatedTodoExecutionServiceTests, TransactionGatedSessionLogServiceTests, TransactionGatedToolRegistryServiceTests, TransactionGatedToolBucketServiceTests, TransactionGatedGraphRagServiceTests, TransactionGatedGitHubCliServiceTests, TransactionGatedGitHubWorkspaceTokenStoreTests, TransactionGatedIssueTodoSyncServiceTests, TransactionGatedStdioRoutingTests, GitHubControllerTests, TurnTransactionsControllerTests, MemoryControllerTests, MemoryMcpToolTests, TodoControllerTests, TodoExecutionMcpToolTests, GraphRagControllerAdHocTests, GraphRagMcpToolTests, SessionLogControllerTests, SessionLogReplaceDeleteControllerTests, ToolRegistryScopeTests, ToolBucketServiceTests, RepoFileServiceTests, PromptTemplateServiceTests, RequirementsDatabaseDocumentServiceTests, TodoExecutionServiceTests, EfTodoServiceTests, DurableTransactionSecurityStorageTests, FederationOperationApplyServiceTests, SeparateTransactionServiceIntegrationTests |
| TR-MCP-TXNAUDIT-001 | ✅ Complete | Technical-Requirements.md, TurnTransactions-Mutation-Endpoint-Audit.md, TurnTransactionPlanArtifactTests, TransactionPubSubReplayWorkerTests, DurableTransactionSecurityStorageTests |
| TR-MCP-TXNCOMPAT-001 | ✅ Complete | Technical-Requirements.md |
| TR-MCP-TXNBYRD-001 | ✅ Complete | Technical-Requirements.md, TurnTransactions-Design-Round2.md, Testing-Requirements.md, TurnTransactionPlanArtifactTests |
| TR-MCP-TXNAIUNIT-001 | ✅ Complete | Technical-Requirements.md |
| TR-MCP-TXNDIAGRAMS-001 | ✅ Complete | Technical-Requirements.md, Quad-Model-Transactional-Diffgram-Plan.md, TurnTransactionPlanArtifactTests, SeparateTransactionServiceIntegrationTests, DiffgramEncryptionIntegrationTests |
| TR-MCP-TXNARCH-001 | ✅ Complete | Technical-Requirements.md, Quad-Model-Transactional-Diffgram-Plan.md, TurnTransactions-Architecture-Round1.md, TurnTransactionPlanArtifactTests |
| TR-MCP-TXNDESIGN-001 | ✅ Complete | Technical-Requirements.md, TurnTransactions-Design-Round2.md, PlanTransactionReviewTests, TurnTransactionPlanArtifactTests |
| TR-MCP-QUAD-001 | ✅ Complete | Technical-Requirements.md, BrainSlotRegistryServiceTests, BrainSlotsControllerTests, BrainSlotClientTests |
| TR-MCP-QUAD-002 | ✅ Complete | Technical-Requirements.md, BrainSlotCredentialResolverTests, BrainSlotInvocationTransactionTests |
| TR-MCP-QUAD-003 | ✅ Complete | Technical-Requirements.md, BrainSlotInvocationTransactionTests, BrainSlotContainmentTests |
| TR-MCP-QUAD-004 | ✅ Complete | Technical-Requirements.md, QuadBrainOrchestrationServiceTests, BrainSlotContainmentTests, TurnTransactionPlanArtifactTests |
| TR-MCP-QUAD-005 | ✅ Complete | Technical-Requirements.md, QuadBrainOrchestrationService, BrainSlotsController, BrainSlotClient, FwhMcpTools, brain-slots.ts, QuadBrainOrchestrationServiceTests, BrainSlotsControllerTests, BrainSlotClientTests, BrainSlotContractArtifactTests, brain-slots.test.ts |
| TR-MCP-QUAD-006 | ✅ Complete | Technical-Requirements.md, QuadBrainOrchestrationService, BrainSlotInvocationService, QuadBrainOrchestrationServiceTests |
| TR-MCP-QUAD-007 | ✅ Complete | Technical-Requirements.md, BrainSlotDefinitionEntity, QuadBrainOrchestrationService, AddBrainSlotWeights migrations, QuadBrainOrchestrationServiceTests |
| TR-MCP-AUTH-010 | ✅ Complete | Technical-Requirements.md, WorkspaceAuthMiddleware, WorkspaceAuthMiddlewareTests |
| TR-MCP-AUTH-011 | ✅ Complete | Technical-Requirements.md, WorkspaceTokenService, WorkspaceAuthMiddleware, WorkspaceTokenServiceTests, WorkspaceAuthMiddlewareTests |
| TR-MCP-HEALTH-002 | ✅ Complete | Technical-Requirements.md, WorkspaceReadinessHealthCheck, Program health checks, WorkspaceReadinessHealthCheckTests, ReadinessAndAuthIntegrationTests |
| TEST-MCP-156 | Tracked | Testing-Requirements.md |
| TEST-MCP-157 | Tracked | Testing-Requirements.md |
| TEST-MCP-158 | ✅ Complete | Testing-Requirements.md, DurableTransactionSecurityStorageTests, TransactionSecurityControllerTests |
| TEST-MCP-159 | ✅ Complete | Testing-Requirements.md, TransactionSecurityControllerTests, DurableTransactionSecurityStorageTests, DiffgramEncryptionIntegrationTests, SeparateTransactionServiceIntegrationTests |
| TEST-MCP-160 | ✅ Complete | SeparateTransactionServiceIntegrationTests, DurableTransactionSecurityStorageTests, TransactionSecurityControllerTests, TransactionSecurityClientTests |
| TEST-MCP-161 | ✅ Complete | TransactionSecurityOptions, TransactionSecurityServiceCollectionExtensions, TurnTransactionCoordinatorTests, TransactionPubSubTests, TransactionPubSubReplayWorkerTests, TurnTransactionsControllerTests, TransactionalTodoWorkflowTests, ClientMutationPolicyTests, FederationControllerTests, FederationControllerPushTests, RequirementsControllerTransactionGateTests, ContextControllerTransactionGateTests, TransactionGatedVoiceConversationServiceTests, TransactionGatedAgentPoolServiceTests, VoiceControllerTests, TransactionGatedMemoryServiceTests, TransactionGatedRepoFileServiceTests, TransactionGatedPromptTemplateServiceTests, TransactionGatedRequirementsDocumentServiceTests, TransactionGatedRequirementsAnalysisServiceTests, TransactionGatedTodoExecutionServiceTests, TransactionGatedTodoMutationServiceTests, TransactionGatedSessionLogServiceTests, TransactionGatedToolRegistryServiceTests, TransactionGatedToolBucketServiceTests, TransactionGatedGraphRagServiceTests, TransactionGatedGitHubCliServiceTests, TransactionGatedGitHubWorkspaceTokenStoreTests, TransactionGatedIssueTodoSyncServiceTests, TransactionGatedStdioRoutingTests, GitHubControllerTests, RequirementsDatabaseDocumentServiceTests, RepoFileServiceTests, PromptTemplateServiceTests, TodoExecutionServiceTests, EfTodoServiceTests, ToolRegistryScopeTests, ToolBucketServiceTests, MemoryControllerTests, MemoryMcpToolTests, TodoControllerTests, TodoExecutionMcpToolTests, GraphRagControllerAdHocTests, GraphRagMcpToolTests, GraphRagToolAdapterTests, ContextClientTests, SessionLogControllerTests, SessionLogReplaceDeleteControllerTests, DurableTransactionSecurityStorageTests, FederationOperationApplyServiceTests, SeparateTransactionServiceIntegrationTests, TurnTransactions-Mutation-Endpoint-Audit.md |
| TEST-MCP-162 | ✅ Complete | Testing-Requirements.md, TurnTransactionPlanArtifactTests, Functional-Requirements.md, Technical-Requirements.md, Requirements-Matrix.md, TR-per-FR-Mapping.md |
| TEST-MCP-163 | ✅ Complete | Testing-Requirements.md, TurnTransactionPlanArtifactTests, Quad-Model-Transactional-Diffgram-Plan.md, TurnTransactions-Mutation-Endpoint-Audit.md |
| TEST-MCP-164 | ✅ Complete | Testing-Requirements.md |
| TEST-MCP-165 | ✅ Complete | Testing-Requirements.md |
| TEST-MCP-166 | ✅ Complete | Testing-Requirements.md, TurnTransactionPlanArtifactTests, Quad-Model-Transactional-Diffgram-Plan.md |
| TEST-MCP-167 | ✅ Complete | Testing-Requirements.md, TurnTransactionPlanArtifactTests, SeparateTransactionServiceIntegrationTests, DiffgramEncryptionIntegrationTests |
| TEST-MCP-168 | ✅ Complete | Testing-Requirements.md, TurnTransactionPlanArtifactTests, TurnTransactionCoordinatorTests, TransactionPubSubTests, TransactionPubSubReplayWorkerTests |
| TEST-MCP-169 | ✅ Complete | Testing-Requirements.md, TurnTransactionPlanArtifactTests, Quad-Model-Transactional-Diffgram-Plan.md |
| TEST-MCP-170 | ✅ Complete | Testing-Requirements.md, TurnTransactionPlanArtifactTests, Quad-Model-Transactional-Diffgram-Plan.md |
| TEST-MCP-171 | ✅ Complete | Testing-Requirements.md, TurnTransactionPlanArtifactTests, TurnTransactions-Architecture-Round1.md |
| TEST-MCP-172 | ✅ Complete | Testing-Requirements.md, TurnTransactionPlanArtifactTests, TurnTransactions-Design-Round2.md |
| TEST-MCP-173 | ✅ Complete | Testing-Requirements.md, TurnTransactionPlanArtifactTests, TurnTransactions-Mutation-Endpoint-Audit.md, Requirements-Matrix.md, TR-per-FR-Mapping.md |
| TEST-MCP-174 | ✅ Complete | Testing-Requirements.md, TurnTransactionPlanArtifactTests |
| TEST-MCP-175 | ✅ Complete | Testing-Requirements.md, BrainSlotRegistryServiceTests |
| TEST-MCP-176 | ✅ Complete | Testing-Requirements.md, BrainSlotsControllerTests, BrainSlotClientTests, BrainSlotContractArtifactTests, brain-slots.test.ts |
| TEST-MCP-177 | ✅ Complete | Testing-Requirements.md, BrainSlotCredentialResolverTests, BrainSlotInvocationTransactionTests |
| TEST-MCP-178 | ✅ Complete | Testing-Requirements.md, BrainSlotInvocationTransactionTests |
| TEST-MCP-179 | ✅ Complete | Testing-Requirements.md, BrainSlotInvocationTransactionTests, BrainSlotContainmentTests |
| TEST-MCP-180 | ✅ Complete | Testing-Requirements.md, QuadBrainOrchestrationServiceTests, BrainSlotContainmentTests |
| TEST-MCP-181 | ✅ Complete | Testing-Requirements.md, QuadBrainOrchestrationServiceTests |
| TEST-MCP-182 | ✅ Complete | Testing-Requirements.md, QuadBrainOrchestrationServiceTests |
| TEST-MCP-183 | ✅ Complete | Testing-Requirements.md, QuadBrainOrchestrationServiceTests |
| TEST-MCP-184 | ✅ Complete | Testing-Requirements.md, BrainSlotsControllerTests, BrainSlotClientTests, BrainSlotContractArtifactTests, brain-slots.test.ts |
| TEST-MCP-185 | ✅ Complete | Testing-Requirements.md, TurnTransactionPlanArtifactTests, Requirements-Matrix.md |
| TEST-MCP-186 | ✅ Complete | Testing-Requirements.md, McpHostedAgentAdapterTests, ServiceCollectionExtensionsTests, HostedAgentWorkflowIntegrationTests |
| TEST-MCP-187 | ✅ Complete | Testing-Requirements.md, McpHostedAgentAdapterTests, HostedAgentWorkflowIntegrationTests |
| TEST-MCP-AUTH-010 | ✅ Complete | Testing-Requirements.md, WorkspaceAuthMiddlewareTests |
| TEST-MCP-AUTH-011 | ✅ Complete | Testing-Requirements.md, WorkspaceAuthMiddlewareTests |
| TEST-MCP-AUTH-012 | ✅ Complete | Testing-Requirements.md, WorkspaceTokenServiceTests |
| TEST-MCP-HEALTH-002 | ✅ Complete | Testing-Requirements.md, WorkspaceReadinessHealthCheckTests |
| TEST-MCP-HEALTH-003 | ✅ Complete | Testing-Requirements.md, ReadinessAndAuthIntegrationTests |
| TEST-MCP-TRACE-LEGACY-001 | ✅ Complete | Testing-Requirements.md, Requirements-Matrix.md, TR-per-FR-Mapping.md |
| TEST-MCP-TRACE-LEGACY-002 | ✅ Complete | Testing-Requirements.md, Requirements-Matrix.md, TR-per-FR-Mapping.md |
| TEST-MCP-TRACE-LEGACY-003 | ✅ Complete | Testing-Requirements.md, Requirements-Matrix.md, TR-per-FR-Mapping.md |
| TEST-MCP-TRACE-REPL-001 | ✅ Complete | Testing-Requirements.md, Requirements-Matrix.md, TR-per-FR-Mapping.md |
| TEST-SUPPORT-010 | ✅ Complete | Testing-Requirements.md, Requirements-Matrix.md, TR-per-FR-Mapping.md |
| FR-MCP-PLUGINCORE-001 | Tracked | Functional-Requirements.md |
| FR-MCP-PLUGINCORE-002 | Tracked | Functional-Requirements.md |
| FR-MCP-PLUGINCORE-003 | Tracked | Functional-Requirements.md |
| FR-MCP-REPL-006 | Tracked | Functional-Requirements.md |
| FR-SUPPORT-014 | Tracked | Functional-Requirements.md |
| FR-SUPPORT-015 | Tracked | Functional-Requirements.md |
| TR-MCP-PLUGINCORE-001 | Tracked | Technical-Requirements.md |
| TR-MCP-PLUGINCORE-002 | Tracked | Technical-Requirements.md |
| TR-MCP-PLUGINCORE-003 | Tracked | Technical-Requirements.md |
| TR-SUPPORT-CORE-014 | Tracked | Technical-Requirements.md |
| TR-SUPPORT-CORE-015 | Tracked | Technical-Requirements.md |
| TEST-MCP-PLUGINCORE-001 | Tracked | Testing-Requirements.md |
| TEST-MCP-PLUGINCORE-002 | Tracked | Testing-Requirements.md |
| TEST-MCP-PLUGINCORE-003 | Tracked | Testing-Requirements.md |
| TEST-SUPPORT-014 | Tracked | Testing-Requirements.md |
| TEST-SUPPORT-015 | Tracked | Testing-Requirements.md |
| TEST-MCP-ACID-001 | Tracked | Testing-Requirements.md |
| TEST-MCP-ACID-002 | Tracked | Testing-Requirements.md |
| TEST-MCP-ACID-003 | Tracked | Testing-Requirements.md |
| TEST-MCP-ACID-004 | Tracked | Testing-Requirements.md |
| TEST-MCP-ACID-005 | Tracked | Testing-Requirements.md |
| TEST-MCP-ACID-006 | Tracked | Testing-Requirements.md |
| FR-MCP-SUBLOG-001 | Tracked | Functional-Requirements.md |
| TR-MCP-SUBLOG-001 | Tracked | Technical-Requirements.md |
| TEST-MCP-SUBLOG-001 | Tracked | Testing-Requirements.md |
| FR-MCP-QBAGENT-001 | Tracked | Functional-Requirements.md |
| TR-MCP-QBAGENT-001 | Tracked | Technical-Requirements.md |
| TEST-MCP-QBAGENT-001 | Tracked | Testing-Requirements.md |
| TEST-MCP-QBAGENTINT-001 | Tracked | Testing-Requirements.md |
| FR-MCP-QBOPENAI-001 | Tracked | Functional-Requirements.md |
| TR-MCP-QBOPENAI-001 | Tracked | Technical-Requirements.md |
| TEST-MCP-QBOPENAI-001 | Tracked | Testing-Requirements.md |
| FR-MCP-QBEXEC-001 | Tracked | Functional-Requirements.md |
| TR-MCP-QBEXEC-001 | Tracked | Technical-Requirements.md |
| TEST-MCP-QBEXEC-001 | Tracked | Testing-Requirements.md |
| TEST-MCP-QBINT-001 | Tracked | Testing-Requirements.md |
| TEST-MCP-QBAGENTINT-002 | Tracked | Testing-Requirements.md |
| TEST-MCP-QBAGENTTOOL-001 | Tracked | Testing-Requirements.md |
| TR-MCP-AGENT-PARITY-020-027 | Tracked | Technical-Requirements.md |
| FR-MCP-QBSEED-001 | Tracked | Functional-Requirements.md |
| TR-MCP-QBSEED-002 | Tracked | Technical-Requirements.md |
| TEST-MCP-QBSEED-001 | Tracked | Testing-Requirements.md |
| TEST-MCP-QBLIVE-001 | Tracked | Testing-Requirements.md |
| TEST-MCP-QBLIVEINT-001 | Tracked | Testing-Requirements.md |
| TEST-MCP-QBOLLAMA-001 | Tracked | Testing-Requirements.md |
| FR-MCP-QBTOOLS-001 | Tracked | Functional-Requirements.md |
| FR-MCP-QBTOOLS-002 | Tracked | Functional-Requirements.md |
| FR-MCP-QBTOOLS-003 | Tracked | Functional-Requirements.md |
| FR-MCP-QBTOOLS-004 | Tracked | Functional-Requirements.md |
| FR-MCP-QBTOOLS-006 | Tracked | Functional-Requirements.md |
| FR-MCP-QBTOOLS-007 | Tracked | Functional-Requirements.md |
| TR-MCP-QBTOOLS-000 | Tracked | Technical-Requirements.md |
| TR-MCP-QBTOOLS-002 | Tracked | Technical-Requirements.md |
| TR-MCP-QBTOOLS-003 | Tracked | Technical-Requirements.md |
| TR-MCP-QBTOOLS-004 | Tracked | Technical-Requirements.md |
| TR-MCP-QBTOOLS-006 | Tracked | Technical-Requirements.md |
| TR-MCP-QBTOOLS-008 | Tracked | Technical-Requirements.md |
| FR-MCP-QBSKILLS-001 | Tracked | Functional-Requirements.md |
| FR-MCP-QBSKILLS-002 | Tracked | Functional-Requirements.md |
| FR-MCP-QBSKILLS-003 | Tracked | Functional-Requirements.md |
| TR-MCP-QBSKILLS-001 | Tracked | Technical-Requirements.md |
| TR-MCP-QBSKILLS-002 | Tracked | Technical-Requirements.md |
| FR-MCP-QBEXEC-002 | Tracked | Functional-Requirements.md |
| FR-MCP-QBEXEC-003 | Tracked | Functional-Requirements.md |
| TR-MCP-QBEXEC-002 | Tracked | Technical-Requirements.md |
| TR-MCP-QBEXEC-003 | Tracked | Technical-Requirements.md |
| TEST-MCP-QBTOOLS-001 | Tracked | Testing-Requirements.md |
| TEST-MCP-QBTOOLS-002 | Tracked | Testing-Requirements.md |
| TEST-MCP-QBTOOLS-003 | Tracked | Testing-Requirements.md |
| TEST-MCP-QBTOOLS-007 | Tracked | Testing-Requirements.md |
| TEST-MCP-QBTOOLSINT-001 | Tracked | Testing-Requirements.md |
| TEST-MCP-QBSKILLS-001 | Tracked | Testing-Requirements.md |
| TEST-MCP-QBSKILLS-002 | Tracked | Testing-Requirements.md |
| TEST-MCP-QBSKILLS-003 | Tracked | Testing-Requirements.md |
| TEST-MCP-QBEXEC-002 | Tracked | Testing-Requirements.md |
| TEST-MCP-QBEXEC-003 | Tracked | Testing-Requirements.md |
| FR-MCP-QUAD-SESSION-001 | Tracked | Functional-Requirements.md |
| TR-MCP-QUAD-SESSION-001 | Tracked | Technical-Requirements.md |
| TEST-MCP-QUAD-SESSION-001 | Tracked | Testing-Requirements.md |
| FR-MCP-QBTOOLS-005 | Tracked | Functional-Requirements.md |
| FR-SUPPORT-010A | Tracked | Functional-Requirements.md |
| FR-SUPPORT-010B | Tracked | Functional-Requirements.md |
| FR-SUPPORT-010C | Tracked | Functional-Requirements.md |
| FR-SUPPORT-010E | Tracked | Functional-Requirements.md |
| FR-SUPPORT-010F | Tracked | Functional-Requirements.md |
| TR-01 | Tracked | Technical-Requirements.md |
| TR-02 | Tracked | Technical-Requirements.md |
| TR-03 | Tracked | Technical-Requirements.md |
| TR-04 | Tracked | Technical-Requirements.md |
| TR-05 | Tracked | Technical-Requirements.md |
| TR-06 | Tracked | Technical-Requirements.md |
| TR-07 | Tracked | Technical-Requirements.md |
| TR-08 | Tracked | Technical-Requirements.md |
| TR-09 | Tracked | Technical-Requirements.md |
| TR-10 | Tracked | Technical-Requirements.md |
| TR-11 | Tracked | Technical-Requirements.md |
| TR-12 | Tracked | Technical-Requirements.md |
| TR-13 | Tracked | Technical-Requirements.md |
| TR-14 | Tracked | Technical-Requirements.md |
| TR-LOC-001 | Tracked | Technical-Requirements.md |
| TR-MCP-MT-003A | Tracked | Technical-Requirements.md |
| TR-MCP-QBSKILLS-003 | Tracked | Technical-Requirements.md |
| TR-MCP-QBTOOLS-001 | Tracked | Technical-Requirements.md |
| TR-MCP-QBTOOLS-005 | Tracked | Technical-Requirements.md |
| TR-MCP-QBTOOLS-007 | Tracked | Technical-Requirements.md |
| TR-PLANNED-013A | Tracked | Technical-Requirements.md |
| TR-SUPPORT-010E | Tracked | Technical-Requirements.md |
| TR-SUPPORT-010F | Tracked | Technical-Requirements.md |
| TR-TEST-001 | Tracked | Technical-Requirements.md |
| TEST-MCP-QBTOOLS-004 | Tracked | Testing-Requirements.md |
| TEST-MCP-QBTOOLS-005 | Tracked | Testing-Requirements.md |
| TEST-MCP-QBTOOLS-006 | Tracked | Testing-Requirements.md |
| TEST-MCP-REPL-007-1 | Tracked | Testing-Requirements.md |
| TEST-MCP-REPL-007-2 | Tracked | Testing-Requirements.md |
| TEST-MCP-REPL-007-3 | Tracked | Testing-Requirements.md |
| TEST-MCP-REPL-007-4 | Tracked | Testing-Requirements.md |
| TEST-MCP-REPL-018 | Tracked | Testing-Requirements.md |
| TEST-MCP-REPL-019 | Tracked | Testing-Requirements.md |
| TEST-MCP-REQAC-PLUGIN-BASH | Tracked | Testing-Requirements.md |
| TEST-MCP-REQACPLUGIN-BASH | Tracked | Testing-Requirements.md |
| TEST-MCP-REQACPLUGIN-CAPTURE | Tracked | Testing-Requirements.md |
| TEST-MCP-REQACPLUGIN-LIVE | Tracked | Testing-Requirements.md |
| TEST-MCP-REQAC-PLUGIN-TS | Tracked | Testing-Requirements.md |
| TEST-MCP-REQACPLUGIN-TS | Tracked | Testing-Requirements.md |
| TEST-SUPPORT-010A-1 | Tracked | Testing-Requirements.md |
| TEST-SUPPORT-010A-2 | Tracked | Testing-Requirements.md |
| TEST-SUPPORT-010B-1 | Tracked | Testing-Requirements.md |
| TEST-SUPPORT-010B-2 | Tracked | Testing-Requirements.md |
| TEST-SUPPORT-010C-1 | Tracked | Testing-Requirements.md |
| TEST-SUPPORT-010C-2 | Tracked | Testing-Requirements.md |
| TEST-SUPPORT-010C-3 | Tracked | Testing-Requirements.md |
| TEST-SUPPORT-010E | Tracked | Testing-Requirements.md |
| TEST-SUPPORT-010F | Tracked | Testing-Requirements.md |
| FR-01 | Tracked | Functional-Requirements.md |
| FR-02 | Tracked | Functional-Requirements.md |
| FR-03 | Tracked | Functional-Requirements.md |
| FR-04 | Tracked | Functional-Requirements.md |
| FR-05 | Tracked | Functional-Requirements.md |
| FR-06 | Tracked | Functional-Requirements.md |
| FR-07 | Tracked | Functional-Requirements.md |
| FR-08 | Tracked | Functional-Requirements.md |
| FR-09 | Tracked | Functional-Requirements.md |
| FR-10 | Tracked | Functional-Requirements.md |
| FR-MCP-138 | Tracked | Functional-Requirements.md |
| FR-MCP-LIVE-CODEX-20260603T2014Z | Tracked | Functional-Requirements.md |
| FR-MCP-LIVE-CODEX-20260603T2015Z | Tracked | Functional-Requirements.md |
| TR-MCP-AIUNIT-001 | Tracked | Technical-Requirements.md |
| TEST-MCP-AIUNIT-001 | Tracked | Testing-Requirements.md |
| FR-MCP-TRIAGE-001 | Complete | Functional-Requirements.md |
| FR-MCP-TRIAGE-002 | Complete | Functional-Requirements.md |
| FR-MCP-TRIAGE-003 | Complete | Functional-Requirements.md |
| FR-MCP-TRIAGE-004 | Complete | Functional-Requirements.md |
| TR-MCP-TRIAGE-001 | Complete | Technical-Requirements.md |
| TR-MCP-TRIAGE-002 | Complete | Technical-Requirements.md |
| TR-MCP-TRIAGE-003 | Complete | Technical-Requirements.md |
| TR-MCP-TRIAGE-004 | Complete | Technical-Requirements.md |
| TR-MCP-REPL-TRIAGE-001 | Complete | Technical-Requirements.md |
| TR-MCP-PLUGIN-TRIAGE-001 | Complete | Technical-Requirements.md |
| TEST-MCP-TRIAGE-001 | Complete | Testing-Requirements.md |
| TEST-MCP-TRIAGE-002 | Complete | Testing-Requirements.md |
| TEST-MCP-TRIAGE-003 | Complete | Testing-Requirements.md |
| TEST-MCP-TRIAGE-004 | Complete | Testing-Requirements.md |
| TEST-MCP-TRIAGE-005 | Complete | Testing-Requirements.md |
| TEST-MCP-TRIAGE-006 | Complete | Testing-Requirements.md |
| FR-MCP-TRIAGE-005 | Complete | Functional-Requirements.md |
| FR-MCP-TRIAGE-006 | Complete | Functional-Requirements.md |
| TR-MCP-TRIAGE-005 | Complete | Technical-Requirements.md |
| TR-MCP-TRIAGE-006 | Complete | Technical-Requirements.md |
| TEST-MCP-TRIAGE-007 | Complete | Testing-Requirements.md |
| TEST-MCP-TRIAGE-008 | Complete | Testing-Requirements.md |
| TEST-MCP-REPL-TRIAGE-001 | Complete | Testing-Requirements.md |
| TEST-MCP-PLUGIN-TRIAGE-001 | Complete | Testing-Requirements.md |
| TEST-MCP-TRIAGE-REQAC-001 | Complete | Testing-Requirements.md |
| FR-TRIAGE-001 | Complete | Functional-Requirements.md |
| TR-TRIAGE-CLIENT-001 | Complete | Technical-Requirements.md |
| TEST-TRIAGE-001 | Complete | Testing-Requirements.md |
| FR-TRIAGE-002 | Complete | Functional-Requirements.md |
| TR-TRIAGE-CLIENT-002 | Complete | Technical-Requirements.md |
| TEST-TRIAGE-002 | Complete | Testing-Requirements.md |
| FR-MCP-PLUGIN-PSONLY-001 | Complete | Functional-Requirements.md |
| FR-MCP-PLUGIN-PSONLY-002 | Complete | Functional-Requirements.md |
| FR-MCP-PLUGIN-PSONLY-003 | Complete | Functional-Requirements.md |
| TR-MCP-PLUGIN-PSONLY-001 | Complete | Technical-Requirements.md |
| TR-MCP-PLUGIN-PSONLY-002 | Complete | Technical-Requirements.md |
| TEST-MCP-PLUGIN-PSONLY-001 | Complete | Testing-Requirements.md |
| TEST-MCP-PLUGIN-PSONLY-002 | Complete | Testing-Requirements.md |
| FR-MCP-MARKER-TRIAGE-001 | Complete | Functional-Requirements.md |
| TR-MCP-MARKER-TRIAGE-001 | Complete | Technical-Requirements.md |
| TEST-MCP-MARKER-TRIAGE-001 | Complete | Testing-Requirements.md |
| TEST-UPD-001 | Tracked | Testing-Requirements.md |
| FR-MCP-REQSCOPE-001 | Planned | Functional-Requirements.md |
| FR-MCP-WORKSPACE-LAYER-001 | Planned | Functional-Requirements.md |
| FR-MCP-REQSCOPE-002 | Planned | Functional-Requirements.md |
| FR-MCP-REQSCOPE-003 | Planned | Functional-Requirements.md |
| FR-MCP-REQSCOPE-004 | Planned | Functional-Requirements.md |
| TR-MCP-REQSCOPE-001 | Planned | Technical-Requirements.md |
| TR-MCP-REQSCOPE-002 | Planned | Technical-Requirements.md |
| TR-MCP-REQSCOPE-003 | Planned | Technical-Requirements.md |
| TR-MCP-REQSCOPE-004 | Planned | Technical-Requirements.md |
| TEST-MCP-REQSCOPE-001 | Planned | Testing-Requirements.md |
| TEST-MCP-WORKSPACE-LAYER-001 | Planned | Testing-Requirements.md |
| TEST-MCP-REQSCOPE-002 | Planned | Testing-Requirements.md |
| TEST-MCP-REQSCOPE-003 | Planned | Testing-Requirements.md |
| TEST-MCP-REQSCOPE-004 | Planned | Testing-Requirements.md |
| TEST-MCP-REQSCOPE-005 | Planned | Testing-Requirements.md |
| TEST-MCP-REQSCOPE-006 | Planned | Testing-Requirements.md |
| TEST-MCP-REQSCOPE-REPL-001 | Planned | Testing-Requirements.md |
| TEST-MCP-REQSCOPE-REQAC-001 | Planned | Testing-Requirements.md |
| FR-MCP-MARKER-REFRESH-001 | Planned | Functional-Requirements.md |
| TR-MCP-MARKER-REFRESH-001 | Planned | Technical-Requirements.md |
| TEST-MCP-MARKER-REFRESH-001 | Planned | Testing-Requirements.md |
| FR-MCP-TODO-CLOSE-001 | Tracked | Functional-Requirements.md |
| TR-MCP-TODO-CLOSE-001 | Tracked | Technical-Requirements.md |
| TEST-MCP-TODO-CLOSE-001 | Tracked | Testing-Requirements.md |
| FR-MCP-WIKIEXPORT-001 | Tracked | Functional-Requirements.md |
| TR-MCP-WIKIEXPORT-001 | Tracked | Technical-Requirements.md |
| TEST-MCP-WIKIEXPORT-001 | Tracked | Testing-Requirements.md |
| FR-MCP-WIKIEXPORT-002 | Tracked | Functional-Requirements.md |
| TR-MCP-WIKIEXPORT-002 | Tracked | Technical-Requirements.md |
| TEST-MCP-WIKIEXPORT-002 | Tracked | Testing-Requirements.md |
| FR-MCP-139 | Tracked | Functional-Requirements.md |
| TR-MCP-QUALITY-001 | Tracked | Technical-Requirements.md |
| TEST-MCP-AIUNIT-002 | Tracked | Testing-Requirements.md |
| FR-MCP-HELP-001 | Complete | Functional-Requirements.md |
| FR-MCP-HELP-002 | Complete | Functional-Requirements.md |
| FR-MCP-HELP-003 | Complete | Functional-Requirements.md |
| FR-MCP-HELP-004 | Complete | Functional-Requirements.md |
| FR-MCP-HELP-005 | Complete | Functional-Requirements.md |
| FR-MCP-HELP-006 | Complete | Functional-Requirements.md |
| FR-MCP-HELP-007 | Complete | Functional-Requirements.md |
| FR-MCP-HELP-008 | Complete | Functional-Requirements.md |
| FR-MCP-HELP-009 | In Progress | Functional-Requirements.md |
| FR-MCP-HELP-010 | Complete | Functional-Requirements.md |
| TR-MCP-HELP-001 | Complete | Technical-Requirements.md |
| TR-MCP-HELP-002 | Complete | Technical-Requirements.md |
| TR-MCP-HELP-003 | Complete | Technical-Requirements.md |
| TR-MCP-HELP-004 | Complete | Technical-Requirements.md |
| TR-MCP-HELP-005 | Complete | Technical-Requirements.md |
| TR-MCP-HELP-006 | Complete | Technical-Requirements.md |
| TR-MCP-HELP-007 | Complete | Technical-Requirements.md |
| TR-MCP-HELP-008 | Complete | Technical-Requirements.md |
| TR-MCP-HELP-009 | Complete | Technical-Requirements.md |
| TEST-MCP-HELP-001 | Complete | Testing-Requirements.md |
| TEST-MCP-HELP-002 | Complete | Testing-Requirements.md |
| TEST-MCP-HELP-003 | Complete | Testing-Requirements.md |
| TEST-MCP-HELP-004 | Complete | Testing-Requirements.md |
| TEST-MCP-HELP-005 | Complete | Testing-Requirements.md |
| TEST-MCP-HELP-006 | In Progress | Testing-Requirements.md |
| TEST-MCP-HELP-007 | In Progress | Testing-Requirements.md |
| TEST-MCP-HELP-008 | Complete | Testing-Requirements.md, AgentHelpWorkflowTests, AgentHelpReplWorkflowTests |
| TEST-MCP-HELP-SEC-001 | Complete | Testing-Requirements.md |
| TEST-MCP-HELP-SEC-002 | Complete | Testing-Requirements.md |
| TEST-MCP-HELP-SEC-003 | Complete | Testing-Requirements.md |
| TEST-MCP-HELP-SEC-004 | Complete | Testing-Requirements.md |
| TEST-MCP-HELP-SEC-005 | Complete | Testing-Requirements.md |
| TEST-MCP-HELP-SEC-006 | Complete | Testing-Requirements.md |
| TEST-MCP-HELP-SEC-007 | Complete | Testing-Requirements.md |
| FR-MCP-DOCFXWIKI-001 | Tracked | Functional-Requirements.md |
| FR-MCP-FILETOOLS-001 | Tracked | Functional-Requirements.md |
| FR-MCP-HELP-011 | Tracked | Functional-Requirements.md |
| FR-MCP-PLUGINCORE-004 | Tracked | Functional-Requirements.md |
| FR-MCP-REPL-009 | Tracked | Functional-Requirements.md |
| FR-MCP-TRANSCRIPT-001 | Tracked | Functional-Requirements.md |
| FR-MCP-TRANSCRIPT-002 | Tracked | Functional-Requirements.md |
| FR-MCP-TRANSCRIPT-003 | Tracked | Functional-Requirements.md |
| FR-MCP-TRANSCRIPT-004 | Tracked | Functional-Requirements.md |
| FR-MCP-TRANSCRIPT-005 | Tracked | Functional-Requirements.md |
| FR-MCP-TRANSCRIPT-006 | Tracked | Functional-Requirements.md |
| FR-MCP-TRANSCRIPT-007 | Tracked | Functional-Requirements.md |
| FR-MCP-TRANSCRIPT-008 | Tracked | Functional-Requirements.md |
| TR-MCP-DOCFXWIKI-001 | Tracked | Technical-Requirements.md |
| TR-MCP-FILETOOLS-001 | Tracked | Technical-Requirements.md |
| TR-MCP-HELP-010 | Tracked | Technical-Requirements.md |
| TR-MCP-PLUGINCORE-004 | Tracked | Technical-Requirements.md |
| TR-MCP-REPL-010 | Tracked | Technical-Requirements.md |
| TR-MCP-TRANSCRIPT-001 | Tracked | Technical-Requirements.md |
| TR-MCP-TRANSCRIPT-002 | Tracked | Technical-Requirements.md |
| TR-MCP-TRANSCRIPT-003 | Tracked | Technical-Requirements.md |
| TR-MCP-TRANSCRIPT-004 | Tracked | Technical-Requirements.md |
| TR-MCP-TRANSCRIPT-005 | Tracked | Technical-Requirements.md |
| TR-MCP-TRANSCRIPT-006 | Tracked | Technical-Requirements.md |
| TR-MCP-TRANSCRIPT-007 | Tracked | Technical-Requirements.md |
| TR-MCP-TRANSCRIPT-008 | Tracked | Technical-Requirements.md |
| TEST-MCP-FILETOOLS-001 | Tracked | Testing-Requirements.md |
| TEST-MCP-FILETOOLSINT-001 | Tracked | Testing-Requirements.md |
| TEST-MCP-PLUGINCORE-004 | Tracked | Testing-Requirements.md |
| TEST-MCP-REPL-025 | Tracked | Testing-Requirements.md |
| TEST-MCP-TRANSCRIPT-001 | Tracked | Testing-Requirements.md |
| TEST-MCP-TRANSCRIPT-002 | Tracked | Testing-Requirements.md |
| TEST-MCP-TRANSCRIPT-003 | Tracked | Testing-Requirements.md |
| TEST-MCP-TRANSCRIPT-004 | Tracked | Testing-Requirements.md |
| TEST-MCP-TRANSCRIPT-005 | Tracked | Testing-Requirements.md |
| TEST-MCP-TRANSCRIPT-006 | Tracked | Testing-Requirements.md |
| TEST-MCP-TRANSCRIPT-007 | Tracked | Testing-Requirements.md |
| TEST-MCP-TRANSCRIPT-008 | Tracked | Testing-Requirements.md |
| TEST-MCP-TRANSCRIPT-009 | Tracked | Testing-Requirements.md |
| TEST-MCP-TRANSCRIPT-010 | Tracked | Testing-Requirements.md |
| FR-MCP-PLUGININT-001 | Tracked | Functional-Requirements.md |
| FR-MCP-SESSIONLOGSAN-001 | Tracked | Functional-Requirements.md |
| TR-MCP-PLUGININT-001 | Tracked | Technical-Requirements.md |
| TR-MCP-SESSIONLOGSAN-001 | Tracked | Technical-Requirements.md |
| TEST-MCP-DOCFXWIKI-001 | Tracked | Testing-Requirements.md |
| TEST-MCP-PLUGININT-001 | Tracked | Testing-Requirements.md |
| TEST-MCP-SESSIONLOGSAN-001 | Tracked | Testing-Requirements.md |
| TR-MCP-DB-006 | Tracked | Technical-Requirements.md |
| TR-MCP-PLUGIN-011 | Tracked | Technical-Requirements.md |
| TR-MCP-PLUGIN-012 | Tracked | Technical-Requirements.md |
| TR-MCP-PLUGIN-013 | Tracked | Technical-Requirements.md |
| TR-MCP-PLUGINCORE-005 | Tracked | Technical-Requirements.md |
| TR-MCP-REPL-011 | Tracked | Technical-Requirements.md |
| TR-MCP-REPL-012 | Tracked | Technical-Requirements.md |
| TR-MCP-REPL-013 | Tracked | Technical-Requirements.md |
| TR-MCP-REQEXPORT-002 | Tracked | Technical-Requirements.md |
| TR-MCP-REQEXPORT-003 | Tracked | Technical-Requirements.md |
| TR-MCP-REQEXPORT-004 | Tracked | Technical-Requirements.md |
| TR-MCP-SESSIONLOG-001 | Tracked | Technical-Requirements.md |
| TR-MCP-SESSIONLOG-002 | Tracked | Technical-Requirements.md |
| TR-MCP-SESSIONLOG-003 | Tracked | Technical-Requirements.md |
| TR-MCP-SESSIONLOGSAN-002 | Tracked | Technical-Requirements.md |
| TR-MCP-TRANSCRIPT-009 | Tracked | Technical-Requirements.md |
| TEST-MCP-BUGTRIAGE-042 | Tracked | Testing-Requirements.md |
| TEST-MCP-BUGTRIAGE-043 | Tracked | Testing-Requirements.md |
| TEST-MCP-DB-006 | Tracked | Testing-Requirements.md |
| TEST-MCP-PLUGIN-011 | Tracked | Testing-Requirements.md |
| TEST-MCP-PLUGIN-012 | Tracked | Testing-Requirements.md |
| TEST-MCP-PLUGIN-013 | Tracked | Testing-Requirements.md |
| TEST-MCP-PLUGINCORE-005 | Tracked | Testing-Requirements.md |
| TEST-MCP-REPL-026 | Tracked | Testing-Requirements.md |
| TEST-MCP-REPL-027 | Tracked | Testing-Requirements.md |
| TEST-MCP-REPL-028 | Tracked | Testing-Requirements.md |
| TEST-MCP-REQEXPORT-002 | Tracked | Testing-Requirements.md |
| TEST-MCP-REQEXPORT-003 | Tracked | Testing-Requirements.md |
| TEST-MCP-REQWS-001 | Tracked | Testing-Requirements.md |
| TEST-MCP-SESSIONLOG-001 | Tracked | Testing-Requirements.md |
| TEST-MCP-SESSIONLOG-002 | Tracked | Testing-Requirements.md |
| TEST-MCP-SESSIONLOG-003 | Tracked | Testing-Requirements.md |
| TEST-MCP-SESSIONLOGSAN-002 | Tracked | Testing-Requirements.md |
| TEST-MCP-TRANSCRIPT-011 | Tracked | Testing-Requirements.md |
| TEST-MCP-TRANSCRIPT-012 | Tracked | Testing-Requirements.md |
| FR-MCP-CLEARSESSION-001 | Tracked | Functional-Requirements.md |
| TR-MCP-CLEARSESSION-001 | Tracked | Technical-Requirements.md |
| TEST-MCP-CLEARSESSION-001 | Tracked | Testing-Requirements.md |
| FR-MCP-REPL-010 | Tracked | Functional-Requirements.md |
| TR-MCP-REPL-014 | Tracked | Technical-Requirements.md |
| TR-MCP-REPL-015 | Tracked | Technical-Requirements.md |
| TR-MCP-SESSIONLOG-005 | Tracked | Technical-Requirements.md |
| TEST-MCP-REPL-029 | Tracked | Testing-Requirements.md |
| TEST-MCP-REPL-030 | Tracked | Testing-Requirements.md |
| TEST-MCP-SESSIONLOG-005 | Tracked | Testing-Requirements.md |
| FR-MCP-140 | Tracked | Functional-Requirements.md |
| FR-MCP-141 | Tracked | Functional-Requirements.md |
| FR-MCP-MARKER-004 | Tracked | Functional-Requirements.md |
| FR-MCP-QBOLLAMA-002 | Tracked | Functional-Requirements.md |
| FR-MCP-TRANSCRIPT-009 | Tracked | Functional-Requirements.md |
| [] | Tracked | Technical-Requirements.md |
| TR-MCP-AGENT-PARITY-020-027 | Tracked | Technical-Requirements.md |
| TR-MCP-MARKER-004 | Tracked | Technical-Requirements.md |
| TR-MCP-QBOLLAMA-002 | Tracked | Technical-Requirements.md |
| TR-MCP-SEC-005 | Tracked | Technical-Requirements.md |
| TR-MCP-SEC-006 | Tracked | Technical-Requirements.md |
| TR-MCP-SVC-002 | Tracked | Technical-Requirements.md |
| TR-MCP-TRANSCRIPT-010 | Tracked | Technical-Requirements.md |
| TR-MCP-TUN-004 | Tracked | Technical-Requirements.md |
| TEST-MCP-189 | Tracked | Testing-Requirements.md |
| TEST-MCP-190 | Tracked | Testing-Requirements.md |
| TEST-MCP-191 | Tracked | Testing-Requirements.md |
| TEST-MCP-192 | Tracked | Testing-Requirements.md |
| TEST-MCP-MARKER-004 | Tracked | Testing-Requirements.md |
| TEST-MCP-QBOLLAMA-002 | Tracked | Testing-Requirements.md |
| TEST-MCP-TRANSCRIPT-013 | Tracked | Testing-Requirements.md |
