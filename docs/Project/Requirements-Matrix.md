# Requirements Matrix (MCP Server)

| Requirement | Status | Source Files |
|-------------|--------|-------------|
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
| TR-LOC-001 | 🔲 Planned | — |
| FR-MCP-026 | ✅ Complete | OidcAuthOptions, Program.cs (JWT Bearer + AgentManager policy), WorkspaceAuthMiddleware, AgentController, AuthConfigController, Setup-McpKeycloak.ps1, setup-mcp-keycloak.sh, McpServer.Director (AuthCommands, OidcAuthService, LoginDialog) |
| FR-MCP-027 | ✅ Complete | Program.cs (startup built-in seeding), AgentController, AgentService, AgentDefaults, AgentDefinitionEntity |
| FR-MCP-028 | 🔲 Planned | AgentController, AgentService, AgentWorkspaceEntity, AgentEventLogEntity, McpDbContext |
| FR-MCP-029 | ✅ Complete | McpServer.Cqrs (Dispatcher, CallContext, CorrelationId, Result, IPipelineBehavior) |
| FR-MCP-030 | ✅ Complete | McpServer.Director (Program, DirectorCommands, AuthCommands, InteractiveCommand, McpHttpClient, OidcAuthService, TokenCache, MainScreen, HealthScreen, AgentScreen, TodoScreen, SessionLogScreen, WorkspaceListScreen, WorkspacePolicyScreen, LoginDialog, ViewModelBinder) |
| FR-MCP-031 | 🔲 Planned | — |
| FR-MCP-032 | 🔲 Planned | — |
| FR-MCP-033 | 🔲 Planned | — |
| FR-MCP-034 | ✅ Complete | IWorkspaceService, MarkerFileService, WorkspaceModels |
| FR-MCP-035 | ✅ Complete | MarkerFileService.DefaultPromptTemplate |
| FR-MCP-036 | 🔲 Planned | — |
| FR-MCP-037 | ✅ Complete | McpServer.Director (Program exec/list-viewmodels), McpServer.Cqrs.Mvvm (IViewModelRegistry) |
| FR-MCP-038 | ✅ Complete | MarkerFileService.DefaultPromptTemplate |
| FR-MCP-039 | 🔲 Planned | — |
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
| TR-MCP-AUDIT-001 | 🔲 Planned | — |
| TR-MCP-POL-001 | 🔲 Planned | — |
| TR-MCP-DTO-001 | ✅ Complete | UnifiedSessionLogDto |
| TR-MCP-CTX-001 | 🔲 Planned | — |
| TR-MCP-MT-001 | ✅ Complete | WorkspaceContext, WorkspaceResolutionMiddleware |
| TR-MCP-MT-002 | ✅ Complete | WorkspaceResolutionMiddleware, WorkspaceTokenService |
| TR-MCP-MT-003 | ✅ Complete | McpDbContext (global query filter), all entities (WorkspaceId) |
| FR-MCP-045 | ✅ Complete | TodoController.MoveAsync, FwhMcpTools.TodoMove, TodoMoveRequest |
| FR-MCP-046 | 🔲 Planned | VoiceController, VoiceConversationService, VoiceConversationOptions, AgentPoolService *(planned)* |
| FR-MCP-047 | ✅ Complete | DesktopProcessLauncher, NativeMethods |
| FR-MCP-048 | ✅ Complete | Program.cs (AddYamlFile), NetEscapades.Configuration.Yaml |
| TR-MCP-TODO-002 | ✅ Complete | TodoController, FwhMcpTools, TodoServiceResolver |
| TR-MCP-VOICE-001–003 | ✅ Complete | VoiceConversationService, VoiceController, VoiceConversationOptions |
| TR-MCP-CFG-004 | ✅ Complete | Program.cs, NetEscapades.Configuration.Yaml |
| TR-MCP-DESKTOP-001 | ✅ Complete | DesktopProcessLauncher, NativeMethods |
| FR-MCP-049 | ✅ Complete | PromptTemplateController, PromptTemplateService, PromptTemplateRenderer, TemplateClient, TemplatesScreen |
| TR-MCP-TPL-001 | ✅ Complete | PromptTemplateService, TemplateStorageOptions |
| TR-MCP-TPL-002 | ✅ Complete | PromptTemplateRenderer |
| TR-MCP-TPL-003 | ✅ Complete | PromptTemplateController, FwhMcpTools |
| TR-MCP-TPL-004 | ✅ Complete | TemplateMessages, \*TemplateQueryHandler, \*TemplateCommandHandler, TemplateApiClientAdapter, TemplateListViewModel, TemplateDetailViewModel, TemplatesScreen |
| FR-MCP-050 | ✅ Complete | IMarkerPromptProvider, FileMarkerPromptProvider, ITodoPromptProvider, TodoPromptProvider, PairingHtmlRenderer |
| TR-MCP-TPL-005 | ✅ Complete | IMarkerPromptProvider, FileMarkerPromptProvider, ITodoPromptProvider, TodoPromptProvider, PairingHtmlRenderer, templates/prompt-templates.yaml |
| FR-MCP-051 | 🔲 Planned | CopilotClientOptions, VoiceConversationOptions, AgentDefaults |
| TR-MCP-CFG-005 | 🔲 Planned | CopilotClientOptions, VoiceConversationOptions, AgentDefaults |
| FR-MCP-052 | 🔲 Planned | AgentPoolOptions *(planned)*, AgentPoolService *(planned)* |
| FR-MCP-053 | 🔲 Planned | AgentPoolQueueService *(planned)*, AgentPoolController *(planned)* |
| FR-MCP-054 | 🔲 Planned | AgentPoolController *(planned)*, AgentPoolNotificationService *(planned)* |
| FR-MCP-055 | 🔲 Planned | AgentPoolIntentResolver *(planned)*, AgentPoolService *(planned)* |
| FR-MCP-056 | 🔲 Planned | PromptTemplateController *(planned extension)*, AgentPoolController *(planned)* |
| FR-MCP-057 | 🔲 Planned | AgentPoolScreen *(planned)*, AgentPoolViewModel *(planned)* |
| FR-MCP-058 | 🔲 Planned | AgentPoolStreamService *(planned)*, VoiceConversationService *(planned extension)* |
| TR-MCP-AGENT-004 | 🔲 Planned | AgentPoolOptions *(planned)*, Program.cs *(planned extension)* |
| TR-MCP-AGENT-005 | 🔲 Planned | IAgentPoolService *(planned)*, AgentPoolService *(planned)*, AgentPoolQueueService *(planned)* |
| TR-MCP-API-002 | 🔲 Planned | AgentPoolController *(planned)*, AgentPoolIntentResolver *(planned)* |
| TR-MCP-API-003 | 🔲 Planned | AgentPoolController *(planned)*, AgentPoolNotificationService *(planned)*, AgentPoolStreamService *(planned)* |
| TR-MCP-TPL-006 | 🔲 Planned | PromptTemplateController *(planned extension)*, PromptTemplateRenderer, AgentPoolController *(planned)* |
| TR-MCP-VOICE-004 | 🔲 Planned | VoiceConversationService *(planned extension)*, AgentPoolStreamService *(planned)* |
| TR-MCP-DIR-004 | 🔲 Planned | AgentPoolScreen *(planned)*, AgentPoolViewModel *(planned)*, McpHttpClient *(planned extension)* |
