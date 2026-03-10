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
| TR-LOC-001 | 🔲 Planned | — |
| FR-MCP-026 | ✅ Complete | OidcAuthOptions, Program.cs (JWT Bearer + AgentManager policy), WorkspaceAuthMiddleware, AgentController, AuthConfigController, Setup-McpKeycloak.ps1, setup-mcp-keycloak.sh, McpServer.Director (AuthCommands, OidcAuthService, LoginDialog) |
| FR-MCP-027 | ✅ Complete | Program.cs (startup built-in seeding), AgentController, AgentService, AgentDefaults, AgentDefinitionEntity |
| FR-MCP-028 | 🔲 Planned | AgentController, AgentService, AgentWorkspaceEntity, AgentEventLogEntity, McpDbContext |
| FR-MCP-029 | ✅ Complete | McpServer.Cqrs (Dispatcher, CallContext, CorrelationId, Result, IPipelineBehavior) |
| FR-MCP-030 | ✅ Complete | McpServer.Director (Program, DirectorCommands, AuthCommands, InteractiveCommand, McpHttpClient, OidcAuthService, TokenCache, MainScreen, HealthScreen, AgentScreen, TodoScreen, SessionLogScreen, WorkspaceListScreen, WorkspacePolicyScreen, LoginDialog, ViewModelBinder) |
| FR-MCP-031 | 🔲 Planned | — |
| FR-MCP-032 | 🔲 Planned | — |
| FR-MCP-033 | ✅ Complete | WorkspaceController (POST /mcpserver/workspace/policy), WorkspacePolicyService, WorkspacePolicyDirectiveParser, McpServerMcpTools.workspace_policy_apply |
| FR-MCP-034 | ✅ Complete | IWorkspaceService, MarkerFileService, WorkspaceModels |
| FR-MCP-035 | ✅ Complete | templates/prompt-templates.yaml |
| FR-MCP-036 | ✅ Complete | AuditedCopilotClient, Program.cs (ICopilotClient decorator), McpStdioHost (ICopilotClient decorator), CopilotServiceCollectionExtensions |
| FR-MCP-037 | ✅ Complete | McpServer.Director (Program exec/list-viewmodels), McpServer.Cqrs.Mvvm (IViewModelRegistry) |
| FR-MCP-038 | ✅ Complete | templates/prompt-templates.yaml |
| FR-MCP-039 | ✅ Complete | Program.cs + McpStdioHost PostConfigure<IngestionOptions>, appsettings.yaml RepoAllowlist, templates/prompt-templates.yaml |
| FR-MCP-040 | ✅ Complete | RequirementsController, RequirementsDocumentService, IRequirementsRepository |
| FR-MCP-041 | ✅ Complete | RequirementsController (/mcpserver/requirements/generate), RequirementsDocumentService, RequirementsDocumentRenderer |
| FR-MCP-042 | ✅ Complete | FwhMcpTools (requirements_* tools), RequirementsDocumentService |
| FR-MCP-043 | ✅ In Progress | WorkspaceResolutionMiddleware, WorkspaceContext, WorkspaceTokenService |
| FR-MCP-044 | ✅ In Progress | McpDbContext (global query filter), all entities (WorkspaceId) |
| TR-MCP-AUTH-001–003 | ✅ Complete | OidcAuthOptions, Program.cs (JwtBearer + AgentManager policy), WorkspaceAuthMiddleware, AgentController, Setup-McpKeycloak.ps1, setup-mcp-keycloak.sh, McpServer.Director (AuthCommands, OidcAuthService) |
| TR-MCP-AGENT-001–003 | ✅ Complete | AgentDefinitionEntity, AgentWorkspaceEntity, AgentEventLogEntity, McpDbContext, AgentDefaults, AgentService, AgentController, Program.cs (startup seeding), WorkspaceAppFactory (primary-only controller exposure) |
| TR-MCP-CQRS-001–005 | ✅ Complete | McpServer.Cqrs (Dispatcher, CallContext, CorrelationId, Result, IPipelineBehavior, ILoggerProvider) |
| TR-MCP-DIR-001–003 | ✅ Complete | McpServer.Director (System.CommandLine CLI, CQRS dispatch, OIDC auth, exec command, Terminal.Gui interactive mode) |
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
| FR-MCP-049 | ✅ Complete | PromptTemplateController, PromptTemplateService, PromptTemplateRenderer, TemplateClient, TemplatesScreen |
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
| FR-MCP-057 | ✅ Complete | AgentPoolClient, Client.Models.AgentPoolModels, McpServerClient.AgentPool, AgentPoolScreen, MainScreen tab wiring |
| FR-MCP-058 | ✅ Complete | AgentPoolController SSE endpoints, AgentPoolService stream subscriptions, VoiceConversationService agent-session reuse/one-shot guard, VoiceController |
| TR-MCP-AGENT-004 | ✅ Complete | AgentPoolOptions, AgentPoolDefinitionOptions, AgentPoolOptionsValidator, Program.cs options validation/DI |
| TR-MCP-AGENT-005 | ✅ Complete | IAgentPoolService, AgentPoolService, AgentPoolController |
| TR-MCP-API-002 | ✅ Complete | AgentPoolController lifecycle/queue/resolve endpoints, AgentPoolService prompt/context routing |
| TR-MCP-API-003 | ✅ Complete | AgentPoolController notifications/jobs SSE, AgentPoolService notification + job stream channels |
| TR-MCP-TPL-006 | ✅ Complete | PromptTemplateController, PromptTemplateRenderer, AgentPoolService template/context prompt resolution |
| TR-MCP-VOICE-004 | ✅ Complete | VoiceConversationService pooled agent reuse + one-shot guard, AgentPoolService voice-runtime dispatch integration |
| TR-MCP-DIR-004 | ✅ Complete | AgentPoolClient, AgentPoolScreen, MainScreen tab integration, DirectorMcpContext typed client usage |
| FR-MCP-059 | 🔲 Planned | McpServer.Support.Mcp services/registries/managers/providers (DI SSOT state flow) |
| FR-MCP-060 | ✅ Complete | McpServer.UI.Core (Messages/Handlers/ViewModels), McpServer.Director (MainScreen, DirectorCommands/AuthCommands, ITabRegistry/DirectorTabRegistry), McpServer.Client adapters |
| FR-MCP-061 | ✅ Complete | TodoValidator, TodoService, SqliteTodoService, SessionLogIdentifierValidator, SessionLogController, SessionLogService |
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
| FR-MCP-066 | ✅ Complete | `McpServer.AgentFramework` (`ServiceCollectionExtensions`, `McpAgentFrameworkOptions`, `AgentFramework/*`, `PowerShellSessions/*`, `SessionLog/*`, `Todo/*`), `McpServer.Client` (`McpServerClient`, `RepoClient`, `DesktopClient`), `McpServer.AgentFramework.SampleHost` (`Program.cs`, `SampleHostPreviewFactory.cs`) |
| TR-MCP-AGENT-006 | ✅ Complete | `ServiceCollectionExtensions`, `McpAgentFrameworkOptions`, `McpAgentFrameworkOptionsValidator`, `IMcpHostedAgent`, `IMcpHostedAgentFactory`, `McpHostedAgent`, `McpHostedAgentRegistration` |
| TR-MCP-AGENT-007 | ✅ Complete | `SessionLogWorkflow`, `SessionLogWorkflowContext`, `SessionLogTurnContext`, `TodoWorkflow`, `IMcpHostedAgent.PowerShellSessions`, `IHostedPowerShellSessionManager`, `McpHostedAgentToolAdapter`, `HostedPowerShellSessionManager`, `HostedPowerShellSessionHost`, `PowerShellSessionCreateResult`, `PowerShellSessionCommandResult`, `PowerShellSessionCloseResult`, `McpServerClient`, `RepoClient`, `DesktopClient`, `McpSessionIdentifierFactory` |
| TEST-MCP-089 | ✅ Complete | `HostedAgentWorkflowIntegrationTests`, `McpHostedAgentAdapterTests`, `DesktopClientTests`, `DesktopControllerTests`, `SessionLogWorkflowTests`, `TodoWorkflowTests`, `ServiceCollectionExtensionsTests`, `PowerShellSessions_ExecuteInteractiveCommand_PreservesHostLocalSessionState` |
| FR-MCP-067 | 🔲 Planned | — |
| TR-MCP-HTTP-002 | 🔲 Planned | — |
| TEST-MCP-090 | 🔲 Planned | — |
| FR-MCP-068 | ✅ Complete | ConfigurationController, AppSettingsFileService, Program.cs (JWT Bearer auth), WorkspaceController |
| TR-MCP-CFG-006 | ✅ Complete | ConfigurationController, AppSettingsFileService, Program.cs (JWT Bearer auth), WorkspaceController |
| TEST-MCP-091 | ✅ Complete | ConfigurationControllerTests, AppSettingsFileServiceTests, ConfigurationAuthorizationPolicyTests |
