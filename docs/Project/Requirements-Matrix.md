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
| FR-MCP-019 | ✅ Complete | ExcludeControllerFeatureProvider, WorkspaceAppFactory |
| FR-MCP-020 | ✅ Complete | WorkspaceProcessManager (IHostedService.StartAsync) |
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
| TR-MCP-WS-002–009 | ✅ Complete | Workspace management |
| TR-MCP-TR-001–003 | ✅ Complete | Tool registry |
| TR-MCP-SEC-001–002 | ✅ Complete | Security |
| TR-MCP-TUN-001–003 | ✅ Complete | Tunneling |
| TR-MCP-HTTP-001 | ✅ Complete | MCP transport |
| TR-MCP-SVC-001 | ✅ Complete | Windows service |
| TR-MCP-REQ-001 | ✅ Complete | AI requirements analysis |
| TR-MCP-DRY-001 | ✅ Active directive | All code and scripts |
| TR-LOC-001 | 🔲 Planned | — |
