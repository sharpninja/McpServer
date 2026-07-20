# SharpNinja.McpServer.Client Endpoint Contract

This file documents every REST/SSE endpoint exposed by the SharpNinja.McpServer.Client package. Endpoint rows are transcribed from src/McpServer.Client/*Client.cs; DTO rows are transcribed from src/McpServer.Client/Models/*.cs plus inline client DTOs.

Endpoint paths are relative to McpServerClientOptions.BaseUrl unless the path starts with /auth, which is intentionally unauthenticated bootstrap surface. Authenticated calls use the shared client auth behavior: X-Api-Key, Authorization: Bearer, and optional X-Workspace-Path.

## Endpoint Map

### Agent

- BanAgentAsync: POST /mcpserver/agents/{agentId}/ban?workspace={workspacePath}
  - Request DTO/body: AgentBanRequest
  - Response DTO/body: AgentMutationResult
  - Source: AgentClient.cs
- DeleteDefinitionAsync: DELETE /mcpserver/agents/definitions/{agentType}
  - Request DTO/body: none
  - Response DTO/body: AgentMutationResult
  - Source: AgentClient.cs
- DeleteWorkspaceAgentAsync: DELETE /mcpserver/agents/{agentId}?workspace={workspacePath}
  - Request DTO/body: none
  - Response DTO/body: AgentMutationResult
  - Source: AgentClient.cs
- GetDefinitionAsync: GET /mcpserver/agents/definitions/{agentType}
  - Request DTO/body: none
  - Response DTO/body: AgentDefinition
  - Source: AgentClient.cs
- GetEventsAsync: GET /mcpserver/agents/{agentId}/events?workspace={workspacePath}&limit={limit}
  - Request DTO/body: none
  - Response DTO/body: AgentEventListResult
  - Source: AgentClient.cs
- GetProcessStatusAsync: GET /mcpserver/agents/{agentId}/process-status?workspace={workspacePath}
  - Request DTO/body: none
  - Response DTO/body: AgentProcessInfo
  - Source: AgentClient.cs
- GetWorkspaceAgentAsync: GET /mcpserver/agents/{agentId}?workspace={workspacePath}
  - Request DTO/body: none
  - Response DTO/body: AgentWorkspaceConfig
  - Source: AgentClient.cs
- LaunchAgentAsync: POST /mcpserver/agents/{agentId}/launch?workspace={workspacePath}
  - Request DTO/body: none
  - Response DTO/body: AgentProcessInfo
  - Source: AgentClient.cs
- ListDefinitionsAsync: GET /mcpserver/agents/definitions
  - Request DTO/body: none
  - Response DTO/body: AgentDefinitionListResult
  - Source: AgentClient.cs
- ListRunningAgentsAsync: GET /mcpserver/agents/running?workspace={workspacePath}
  - Request DTO/body: none
  - Response DTO/body: AgentRunningListResult
  - Source: AgentClient.cs
- ListWorkspaceAgentsAsync: GET /mcpserver/agents?workspace={workspacePath}
  - Request DTO/body: none
  - Response DTO/body: AgentWorkspaceListResult
  - Source: AgentClient.cs
- LogEventAsync: POST /mcpserver/agents/{agentId}/events?workspace={workspacePath}
  - Request DTO/body: AgentEventRequest
  - Response DTO/body: AgentMutationResult
  - Source: AgentClient.cs
- SeedDefinitionsAsync: POST /mcpserver/agents/definitions/seed
  - Request DTO/body: none
  - Response DTO/body: AgentSeedDefaultsResult
  - Source: AgentClient.cs
  - Note: Compatibility alias for SeedDefaultsAsync.
- SeedDefaultsAsync: POST /mcpserver/agents/definitions/seed
  - Request DTO/body: none
  - Response DTO/body: AgentSeedDefaultsResult
  - Source: AgentClient.cs
- StopAgentAsync: POST /mcpserver/agents/{agentId}/stop?workspace={workspacePath}
  - Request DTO/body: none
  - Response DTO/body: AgentMutationResult
  - Source: AgentClient.cs
- UnbanAgentAsync: POST /mcpserver/agents/{agentId}/unban?workspace={workspacePath}&global={global}
  - Request DTO/body: none
  - Response DTO/body: AgentMutationResult
  - Source: AgentClient.cs
- UpsertDefinitionAsync: POST /mcpserver/agents/definitions
  - Request DTO/body: AgentDefinitionRequest
  - Response DTO/body: AgentMutationResult
  - Source: AgentClient.cs
- UpsertWorkspaceAgentAsync: POST /mcpserver/agents/{agentId}?workspace={workspacePath}
  - Request DTO/body: AgentWorkspaceRequest
  - Response DTO/body: AgentMutationResult
  - Source: AgentClient.cs
- ValidateAsync: GET /mcpserver/agents/validate?workspace={workspacePath}
  - Request DTO/body: none
  - Response DTO/body: AgentValidateResult
  - Source: AgentClient.cs

### AgentPool

- CancelQueueItemAsync: POST /mcpserver/agent-pool/queue/{jobId}/cancel
  - Request DTO/body: none
  - Response DTO/body: AgentPoolMutationResult
  - Source: AgentPoolClient.cs
- ConnectAsync: POST /mcpserver/agent-pool/agents/{agentName}/connect
  - Request DTO/body: none
  - Response DTO/body: AgentPoolConnectResult
  - Source: AgentPoolClient.cs
- ConnectDefaultAsync: POST /mcpserver/agent-pool/connect
  - Request DTO/body: none
  - Response DTO/body: AgentPoolConnectResult
  - Source: AgentPoolClient.cs
- EnqueueOneShotAsync: POST /mcpserver/agent-pool/queue/one-shot
  - Request DTO/body: AgentPoolOneShotRequest
  - Response DTO/body: AgentPoolEnqueueResult
  - Source: AgentPoolClient.cs
- MoveQueueItemDownAsync: POST /mcpserver/agent-pool/queue/{jobId}/move-down
  - Request DTO/body: none
  - Response DTO/body: AgentPoolMutationResult
  - Source: AgentPoolClient.cs
- MoveQueueItemUpAsync: POST /mcpserver/agent-pool/queue/{jobId}/move-up
  - Request DTO/body: none
  - Response DTO/body: AgentPoolMutationResult
  - Source: AgentPoolClient.cs
- RecycleAgentAsync: POST /mcpserver/agent-pool/agents/{agentName}/recycle
  - Request DTO/body: none
  - Response DTO/body: AgentPoolMutationResult
  - Source: AgentPoolClient.cs
- RemoveQueueItemAsync: DELETE /mcpserver/agent-pool/queue/{jobId}
  - Request DTO/body: none
  - Response DTO/body: AgentPoolMutationResult
  - Source: AgentPoolClient.cs
- ResolvePromptAsync: POST /mcpserver/agent-pool/queue/resolve
  - Request DTO/body: AgentPoolOneShotRequest
  - Response DTO/body: AgentPoolPromptResolutionResult
  - Source: AgentPoolClient.cs
- StartAgentAsync: POST /mcpserver/agent-pool/agents/{agentName}/start
  - Request DTO/body: none
  - Response DTO/body: AgentPoolMutationResult
  - Source: AgentPoolClient.cs
- StopAgentAsync: POST /mcpserver/agent-pool/agents/{agentName}/stop
  - Request DTO/body: none
  - Response DTO/body: AgentPoolMutationResult
  - Source: AgentPoolClient.cs
- StreamJobAsync: GET /mcpserver/agent-pool/jobs/{jobId}/stream
  - Request DTO/body: none
  - Response DTO/body: SSE stream of AgentPoolJobStreamEvent
  - Source: AgentPoolClient.cs

### AuthConfig

- GetConfigAsync: GET /auth/config
  - Request DTO/body: none
  - Response DTO/body: AuthConfigResponse
  - Source: AuthConfigClient.cs
- RequestDeviceAuthorizationAsync: POST /auth/device
  - Request DTO/body: AuthDeviceAuthorizationRequest (form-url-encoded)
  - Response DTO/body: AuthDeviceAuthorizationResponse
  - Source: AuthConfigClient.cs
- RequestTokenAsync: POST /auth/token
  - Request DTO/body: AuthTokenRequest (form-url-encoded)
  - Response DTO/body: AuthTokenResponse
  - Source: AuthConfigClient.cs

### BrainSlot

- DeleteAsync: DELETE /mcpserver/brain-slots/{slotId}
  - Request DTO/body: none
  - Response DTO/body: BrainSlotDto
  - Source: BrainSlotClient.cs
- DisableAsync: POST /mcpserver/brain-slots/{slotId}/disable
  - Request DTO/body: none
  - Response DTO/body: BrainSlotDto
  - Source: BrainSlotClient.cs
- EnableAsync: POST /mcpserver/brain-slots/{slotId}/enable?replaceExisting={replaceExisting}
  - Request DTO/body: none
  - Response DTO/body: BrainSlotDto
  - Source: BrainSlotClient.cs
- GetAsync: GET /mcpserver/brain-slots/{slotId}
  - Request DTO/body: none
  - Response DTO/body: BrainSlotDto
  - Source: BrainSlotClient.cs
- GetStatusAsync: GET /mcpserver/brain-slots/status
  - Request DTO/body: none
  - Response DTO/body: BrainSlotStatusResponse
  - Source: BrainSlotClient.cs
- InvokeAsync: POST /mcpserver/brain-slots/{slotId}/invoke
  - Request DTO/body: BrainSlotInvokeRequest
  - Response DTO/body: BrainSlotInvokeResponse
  - Source: BrainSlotClient.cs
- OrchestrateAsync: POST /mcpserver/brain-slots/orchestrate
  - Request DTO/body: QuadBrainOrchestrationRequest
  - Response DTO/body: QuadBrainOrchestrationResponse
  - Source: BrainSlotClient.cs
- ReconcileAotAsync: POST /mcpserver/brain-slots/aot/reconcile
  - Request DTO/body: AotReconciliationRequest
  - Response DTO/body: AotReconciliationResponse
  - Source: BrainSlotClient.cs
- UpdateWeightsAsync: POST /mcpserver/brain-slots/weights/update
  - Request DTO/body: QuadBrainWeightUpdateRequest
  - Response DTO/body: QuadBrainWeightUpdateResponse
  - Source: BrainSlotClient.cs
- UpsertAsync: PUT /mcpserver/brain-slots/{slotId}
  - Request DTO/body: UpsertBrainSlotRequest
  - Response DTO/body: BrainSlotDto
  - Source: BrainSlotClient.cs

### Context

- GraphRagCreateEntityAsync: POST /mcpserver/graphrag/entities
  - Request DTO/body: GraphEntityRequest
  - Response DTO/body: GraphEntityResult
  - Source: ContextClient.cs
- GraphRagCreateRelationshipAsync: POST /mcpserver/graphrag/relationships
  - Request DTO/body: GraphRelationshipRequest
  - Response DTO/body: GraphRelationshipResult
  - Source: ContextClient.cs
- GraphRagDeleteDocumentAsync: DELETE /mcpserver/graphrag/documents/{documentId}
  - Request DTO/body: none
  - Response DTO/body: GraphRagDocumentDeleteResult
  - Source: ContextClient.cs
- GraphRagDeleteEntityAsync: DELETE /mcpserver/graphrag/entities/{entityId}
  - Request DTO/body: none
  - Response DTO/body: Task
  - Source: ContextClient.cs
- GraphRagDeleteRelationshipAsync: DELETE /mcpserver/graphrag/relationships/{relationshipId}
  - Request DTO/body: none
  - Response DTO/body: Task
  - Source: ContextClient.cs
- GraphRagGetDocumentChunksAsync: GET /mcpserver/graphrag/documents/{documentId}/chunks
  - Request DTO/body: none
  - Response DTO/body: GraphRagDocumentChunksResult
  - Source: ContextClient.cs
- GraphRagGetEntityAsync: GET /mcpserver/graphrag/entities/{entityId}
  - Request DTO/body: none
  - Response DTO/body: GraphEntityResult
  - Source: ContextClient.cs
- GraphRagGetRelationshipAsync: GET /mcpserver/graphrag/relationships/{relationshipId}
  - Request DTO/body: none
  - Response DTO/body: GraphRelationshipResult
  - Source: ContextClient.cs
- GraphRagIndexAsync: POST /mcpserver/graphrag/index
  - Request DTO/body: GraphRagIndexRequest
  - Response DTO/body: GraphRagStatusResult
  - Source: ContextClient.cs
- GraphRagIngestTextAsync: POST /mcpserver/graphrag/documents/ingest
  - Request DTO/body: GraphRagIngestTextRequest
  - Response DTO/body: GraphRagIngestTextResult
  - Source: ContextClient.cs
- GraphRagListDocumentsAsync: GET /mcpserver/graphrag/documents?skip={skip}&take={take}
  - Request DTO/body: none
  - Response DTO/body: GraphRagDocumentListResult
  - Source: ContextClient.cs
- GraphRagListEntitiesAsync: GET /mcpserver/graphrag/entities?skip={skip}&take={take}
  - Request DTO/body: none
  - Response DTO/body: GraphEntityListResult
  - Source: ContextClient.cs
- GraphRagListRelationshipsAsync: GET /mcpserver/graphrag/relationships?skip={skip}&take={take}
  - Request DTO/body: none
  - Response DTO/body: GraphRelationshipListResult
  - Source: ContextClient.cs
- GraphRagQueryAsync: POST /mcpserver/graphrag/query
  - Request DTO/body: GraphRagQueryRequest
  - Response DTO/body: GraphRagQueryResult
  - Source: ContextClient.cs
- GraphRagStatusAsync: GET /mcpserver/graphrag/status
  - Request DTO/body: none
  - Response DTO/body: GraphRagStatusResult
  - Source: ContextClient.cs
- GraphRagUpdateEntityAsync: PUT /mcpserver/graphrag/entities/{entityId}
  - Request DTO/body: GraphEntityRequest
  - Response DTO/body: GraphEntityResult
  - Source: ContextClient.cs
- GraphRagUpdateRelationshipAsync: PUT /mcpserver/graphrag/relationships/{relationshipId}
  - Request DTO/body: GraphRelationshipRequest
  - Response DTO/body: GraphRelationshipResult
  - Source: ContextClient.cs
- IngestWebsiteAsync: POST /mcpserver/context/ingest-website
  - Request DTO/body: WebsiteIngestRequest
  - Response DTO/body: WebsiteIngestResult
  - Source: ContextClient.cs
- ListSourcesAsync: GET /mcpserver/context/sources
  - Request DTO/body: none
  - Response DTO/body: ContextSourcesResult
  - Source: ContextClient.cs
- PackAsync: POST /mcpserver/context/pack
  - Request DTO/body: ContextPackRequest
  - Response DTO/body: ContextPack
  - Source: ContextClient.cs
- RebuildIndexAsync: POST /mcpserver/context/rebuild-index
  - Request DTO/body: none
  - Response DTO/body: RebuildIndexResult
  - Source: ContextClient.cs
- SearchAsync: POST /mcpserver/context/search
  - Request DTO/body: ContextSearchRequest
  - Response DTO/body: ContextSearchResult
  - Source: ContextClient.cs
- StreamIngestWebsiteAsync: POST /mcpserver/context/ingest-website/stream
  - Request DTO/body: WebsiteIngestRequest
  - Response DTO/body: SSE stream of string
  - Source: ContextClient.cs

### Desktop

- LaunchAsync: POST /mcpserver/desktop/launch
  - Request DTO/body: DesktopLaunchRequest
  - Response DTO/body: DesktopLaunchResult
  - Source: DesktopClient.cs

### Diagnostic

- GetAppSettingsPathAsync: GET /mcpserver/diagnostic/appsettings-path
  - Request DTO/body: none
  - Response DTO/body: DiagnosticAppSettingsPathResult
  - Source: DiagnosticClient.cs
- GetExecutionPathAsync: GET /mcpserver/diagnostic/execution-path
  - Request DTO/body: none
  - Response DTO/body: DiagnosticExecutionPathResult
  - Source: DiagnosticClient.cs

### EventStream

- SubscribeAsync: GET /mcpserver/events?category={category}
  - Request DTO/body: none
  - Response DTO/body: SSE stream of ChangeEvent
  - Source: EventStreamClient.cs

### Federation

- AcknowledgeOperationAsync: POST /mcpserver/federation/operations/{operationId}/ack
  - Request DTO/body: FederationOperationAckRequest
  - Response DTO/body: FederationOperationResponse
  - Source: FederationClient.cs
- AcknowledgeSyncAsync: POST /mcpserver/federation/sync/{sequence}/ack
  - Request DTO/body: FederationSyncAckRequest
  - Response DTO/body: FederationOperationResponse
  - Source: FederationClient.cs
- AddTargetAsync: POST /mcpserver/federation/targets
  - Request DTO/body: FederationTargetAddRequest
  - Response DTO/body: FederationTargetInfo
  - Source: FederationClient.cs
- ClearDefaultTargetAsync: DELETE /mcpserver/federation/targets/default
  - Request DTO/body: none
  - Response DTO/body: FederationStatusResponse
  - Source: FederationClient.cs
- DisableAsync: POST /mcpserver/federation/disable
  - Request DTO/body: none
  - Response DTO/body: FederationStatusResponse
  - Source: FederationClient.cs
- DiscoverFromTunnelsAsync: POST /mcpserver/federation/targets/discover-from-tunnels
  - Request DTO/body: none
  - Response DTO/body: TunnelDiscoveryResult
  - Source: FederationClient.cs
- EnableAsync: POST /mcpserver/federation/enable
  - Request DTO/body: none
  - Response DTO/body: FederationStatusResponse
  - Source: FederationClient.cs
- EnrollProxyAsync: POST /mcpserver/federation/proxies/enroll
  - Request DTO/body: FederationEnrollmentRequest
  - Response DTO/body: FederationEnrollmentResponse
  - Source: FederationClient.cs
- GetConnectionAsync: GET /mcpserver/federation/connection?workspaceName={workspaceName}
  - Request DTO/body: none
  - Response DTO/body: FederationConnectionInfo
  - Source: FederationClient.cs
- GetQueueStatusAsync: GET /mcpserver/federation/queue?proxyId={proxyId}
  - Request DTO/body: none
  - Response DTO/body: FederationQueueStatusResponse
  - Source: FederationClient.cs
- GetStatusAsync: GET /mcpserver/federation/status
  - Request DTO/body: none
  - Response DTO/body: FederationStatusResponse
  - Source: FederationClient.cs
- HeartbeatAsync: POST /mcpserver/federation/proxies/{proxyId}/heartbeat
  - Request DTO/body: FederationHeartbeatRequest
  - Response DTO/body: FederationHeartbeatResponse
  - Source: FederationClient.cs
- PushAsync: POST /mcpserver/federation/push
  - Request DTO/body: FederationPushRequest
  - Response DTO/body: FederationPushResult
  - Source: FederationClient.cs
- RecordEnvelopeAsync: POST /mcpserver/federation/envelopes
  - Request DTO/body: FederationExecutionEnvelope
  - Response DTO/body: FederationOperationResponse
  - Source: FederationClient.cs
- RecordOperationAsync: POST /mcpserver/federation/operations
  - Request DTO/body: FederationOperationRequest
  - Response DTO/body: FederationOperationResponse
  - Source: FederationClient.cs
- RegisterWorkspaceAsync: POST /mcpserver/federation/proxies/{proxyId}/workspaces
  - Request DTO/body: FederationWorkspaceRegistrationRequest
  - Response DTO/body: FederationWorkspaceInfo
  - Source: FederationClient.cs
- RemoveRouteAsync: DELETE /mcpserver/federation/routes
  - Request DTO/body: WorkspaceRouteRequest
  - Response DTO/body: Task<HttpStatusCode>
  - Source: FederationClient.cs
- RemoveTargetAsync: DELETE /mcpserver/federation/targets/{name}
  - Request DTO/body: none
  - Response DTO/body: Task<HttpStatusCode>
  - Source: FederationClient.cs
- ResolveConflictAsync: POST /mcpserver/federation/conflicts/{conflictId}/resolve
  - Request DTO/body: FederationConflictResolutionRequest
  - Response DTO/body: FederationConflictInfo
  - Source: FederationClient.cs
- SetDefaultTargetAsync: POST /mcpserver/federation/targets/{name}/set-default
  - Request DTO/body: none
  - Response DTO/body: FederationStatusResponse
  - Source: FederationClient.cs

### GitHub

- CancelWorkflowRunAsync: POST /mcpserver/gh/actions/runs/{runId}/cancel
  - Request DTO/body: none
  - Response DTO/body: GitHubOperationResult
  - Source: GitHubClient.cs
- CloseIssueAsync: POST /mcpserver/gh/issues/{number}/close?reason={reason}
  - Request DTO/body: none
  - Response DTO/body: GitHubMutationResult
  - Source: GitHubClient.cs
- CommentOnIssueAsync: POST /mcpserver/gh/issues/{number}/comments
  - Request DTO/body: GitHubCommentRequest
  - Response DTO/body: GitHubMutationResult
  - Source: GitHubClient.cs
- CommentOnPullAsync: POST /mcpserver/gh/pulls/{number}/comments
  - Request DTO/body: GitHubCommentRequest
  - Response DTO/body: GitHubMutationResult
  - Source: GitHubClient.cs
- CreateIssueAsync: POST /mcpserver/gh/issues
  - Request DTO/body: GitHubIssueRequest
  - Response DTO/body: GitHubCreateIssueResult
  - Source: GitHubClient.cs
- DeleteAuthTokenAsync: DELETE /mcpserver/gh/auth/token
  - Request DTO/body: none
  - Response DTO/body: GitHubOperationResult
  - Source: GitHubClient.cs
- GetAuthorizeUrlAsync: GET /mcpserver/gh/oauth/authorize-url?state={state}
  - Request DTO/body: none
  - Response DTO/body: GitHubAuthorizeUrlResult
  - Source: GitHubClient.cs
- GetAuthStatusAsync: GET /mcpserver/gh/auth/status
  - Request DTO/body: none
  - Response DTO/body: GitHubAuthStatusResult
  - Source: GitHubClient.cs
- GetIssueAsync: GET /mcpserver/gh/issues/{number}
  - Request DTO/body: none
  - Response DTO/body: GitHubIssueDetail
  - Source: GitHubClient.cs
- GetOAuthConfigAsync: GET /mcpserver/gh/oauth/config
  - Request DTO/body: none
  - Response DTO/body: GitHubOAuthConfigResult
  - Source: GitHubClient.cs
- GetWorkflowRunAsync: GET /mcpserver/gh/actions/runs/{runId}
  - Request DTO/body: none
  - Response DTO/body: GitHubWorkflowRunDetail
  - Source: GitHubClient.cs
- ListIssuesAsync: GET /mcpserver/gh/issues?state={state}&limit={limit}
  - Request DTO/body: none
  - Response DTO/body: GitHubIssueListResult
  - Source: GitHubClient.cs
- ListLabelsAsync: GET /mcpserver/gh/labels
  - Request DTO/body: none
  - Response DTO/body: GitHubLabelsResult
  - Source: GitHubClient.cs
- ListPullsAsync: GET /mcpserver/gh/pulls?state={state}&limit={limit}
  - Request DTO/body: none
  - Response DTO/body: GitHubPullListResult
  - Source: GitHubClient.cs
- ListWorkflowRunsAsync: GET /mcpserver/gh/actions/runs?branch={branch}&status={status}&event={eventName}&workflow={workflow}&limit={limit}
  - Request DTO/body: none
  - Response DTO/body: GitHubWorkflowRunListResult
  - Source: GitHubClient.cs
- ReopenIssueAsync: POST /mcpserver/gh/issues/{number}/reopen
  - Request DTO/body: none
  - Response DTO/body: GitHubMutationResult
  - Source: GitHubClient.cs
- RerunWorkflowRunAsync: POST /mcpserver/gh/actions/runs/{runId}/rerun
  - Request DTO/body: none
  - Response DTO/body: GitHubOperationResult
  - Source: GitHubClient.cs
- SetAuthTokenAsync: PUT /mcpserver/gh/auth/token
  - Request DTO/body: GitHubAuthTokenUpsertRequest
  - Response DTO/body: GitHubOperationResult
  - Source: GitHubClient.cs
- SyncFromGitHubAsync: POST /mcpserver/gh/issues/sync/from-github?state={state}&limit={limit}
  - Request DTO/body: none
  - Response DTO/body: IssueSyncResult
  - Source: GitHubClient.cs
- SyncIssueAsync: POST /mcpserver/gh/issues/{number}/sync?direction={direction}
  - Request DTO/body: none
  - Response DTO/body: SingleIssueSyncResult
  - Source: GitHubClient.cs
- SyncToGitHubAsync: POST /mcpserver/gh/issues/sync/to-github
  - Request DTO/body: none
  - Response DTO/body: IssueSyncResult
  - Source: GitHubClient.cs
- UpdateIssueAsync: PUT /mcpserver/gh/issues/{number}
  - Request DTO/body: GitHubIssueUpdateRequest
  - Response DTO/body: GitHubMutationResult
  - Source: GitHubClient.cs

### GraphRag

- CreateEntityAsync: POST /mcpserver/graphrag/entities
  - Request DTO/body: GraphEntityRequest
  - Response DTO/body: GraphEntityResult
  - Source: GraphRagClient.cs
- CreateRelationshipAsync: POST /mcpserver/graphrag/relationships
  - Request DTO/body: GraphRelationshipRequest
  - Response DTO/body: GraphRelationshipResult
  - Source: GraphRagClient.cs
- DeleteDocumentAsync: DELETE /mcpserver/graphrag/documents/{documentId}
  - Request DTO/body: none
  - Response DTO/body: GraphRagDocumentDeleteResult
  - Source: GraphRagClient.cs
- DeleteEntityAsync: DELETE /mcpserver/graphrag/entities/{entityId}
  - Request DTO/body: none
  - Response DTO/body: Task
  - Source: GraphRagClient.cs
- DeleteRelationshipAsync: DELETE /mcpserver/graphrag/relationships/{relationshipId}
  - Request DTO/body: none
  - Response DTO/body: Task
  - Source: GraphRagClient.cs
- GetDocumentChunksAsync: GET /mcpserver/graphrag/documents/{documentId}/chunks
  - Request DTO/body: none
  - Response DTO/body: GraphRagDocumentChunksResult
  - Source: GraphRagClient.cs
- GetEntityAsync: GET /mcpserver/graphrag/entities/{entityId}
  - Request DTO/body: none
  - Response DTO/body: GraphEntityResult
  - Source: GraphRagClient.cs
- GetRelationshipAsync: GET /mcpserver/graphrag/relationships/{relationshipId}
  - Request DTO/body: none
  - Response DTO/body: GraphRelationshipResult
  - Source: GraphRagClient.cs
- IndexAsync: POST /mcpserver/graphrag/index
  - Request DTO/body: GraphRagIndexRequest
  - Response DTO/body: GraphRagStatusResult
  - Source: GraphRagClient.cs
- IngestTextAsync: POST /mcpserver/graphrag/documents/ingest
  - Request DTO/body: GraphRagIngestTextRequest
  - Response DTO/body: GraphRagIngestTextResult
  - Source: GraphRagClient.cs
- ListDocumentsAsync: GET /mcpserver/graphrag/documents?skip={skip}&take={take}
  - Request DTO/body: none
  - Response DTO/body: GraphRagDocumentListResult
  - Source: GraphRagClient.cs
- ListEntitiesAsync: GET /mcpserver/graphrag/entities?skip={skip}&take={take}
  - Request DTO/body: none
  - Response DTO/body: GraphEntityListResult
  - Source: GraphRagClient.cs
- ListRelationshipsAsync: GET /mcpserver/graphrag/relationships?skip={skip}&take={take}
  - Request DTO/body: none
  - Response DTO/body: GraphRelationshipListResult
  - Source: GraphRagClient.cs
- QueryAsync: POST /mcpserver/graphrag/query
  - Request DTO/body: GraphRagQueryRequest
  - Response DTO/body: GraphRagQueryResult
  - Source: GraphRagClient.cs
- StatusAsync: GET /mcpserver/graphrag/status
  - Request DTO/body: none
  - Response DTO/body: GraphRagStatusResult
  - Source: GraphRagClient.cs
- UpdateEntityAsync: PUT /mcpserver/graphrag/entities/{entityId}
  - Request DTO/body: GraphEntityRequest
  - Response DTO/body: GraphEntityResult
  - Source: GraphRagClient.cs
- UpdateRelationshipAsync: PUT /mcpserver/graphrag/relationships/{relationshipId}
  - Request DTO/body: GraphRelationshipRequest
  - Response DTO/body: GraphRelationshipResult
  - Source: GraphRagClient.cs

### Health

- GetAliveAsync: GET /alive
  - Request DTO/body: none
  - Response DTO/body: HealthCheckResult
  - Source: HealthClient.cs
- GetAsync: GET /health
  - Request DTO/body: none
  - Response DTO/body: HealthCheckResult
  - Source: HealthClient.cs
- GetMarkerFileTimestampAsync: GET /marker-file-timestamp?repoPath={repoPath}
  - Request DTO/body: none
  - Response DTO/body: MarkerFileTimestampResult
  - Source: HealthClient.cs
- GetReadyAsync: GET /ready
  - Request DTO/body: none
  - Response DTO/body: HealthCheckResult
  - Source: HealthClient.cs
- GetServerStartupAsync: GET /server-startup-utc
  - Request DTO/body: none
  - Response DTO/body: ServerStartupResult
  - Source: HealthClient.cs

### KeyServer

- GetManifestAsync: GET /mcpserver/keyserver/manifests/{transactionId}
  - Request DTO/body: none
  - Response DTO/body: TransactionManifestTraceRecord
  - Source: KeyServerClient.cs
- GetManifestReportAsync: GET /mcpserver/keyserver/manifests/report?publisherPartyId={publisherPartyId}&subscriberPartyId={subscriberPartyId}&status={status}&fromUtc={fromUtc}&toUtc={toUtc}&limit={limit}
  - Request DTO/body: none
  - Response DTO/body: TransactionManifestTraceReport
  - Source: KeyServerClient.cs
- GetPartyKeyAsync: GET /mcpserver/keyserver/parties/{partyId}/keys/{keyId}
  - Request DTO/body: none
  - Response DTO/body: PartyKeyDescriptor
  - Source: KeyServerClient.cs
- RegisterPartyAsync: POST /mcpserver/keyserver/parties
  - Request DTO/body: PartyRegistrationRequest
  - Response DTO/body: PartyRegistrationResponse
  - Source: KeyServerClient.cs
- SignManifestAsync: POST /mcpserver/keyserver/manifests/sign
  - Request DTO/body: TransactionManifestSignRequest
  - Response DTO/body: TransactionManifestSignResponse
  - Source: KeyServerClient.cs
- VerifyManifestAsync: POST /mcpserver/keyserver/manifests/verify
  - Request DTO/body: TransactionManifestVerifyRequest
  - Response DTO/body: TransactionManifestVerifyResponse
  - Source: KeyServerClient.cs

### Memory

- AddAsync: POST /mcpserver/memory
  - Request DTO/body: MemoryAddRequest
  - Response DTO/body: MemoryMutationResult
  - Source: MemoryClient.cs
- GetAsync: GET /mcpserver/memory/{id}
  - Request DTO/body: none
  - Response DTO/body: MemoryItem
  - Source: MemoryClient.cs
- ListAsync: GET /mcpserver/memory?scope={scope}&category={category}&keyword={keyword}
  - Request DTO/body: none
  - Response DTO/body: MemoryQueryResult
  - Source: MemoryClient.cs
- RemoveAsync: DELETE /mcpserver/memory/{id}
  - Request DTO/body: none
  - Response DTO/body: MemoryMutationResult
  - Source: MemoryClient.cs
- UpdateAsync: PUT /mcpserver/memory/{id}
  - Request DTO/body: MemoryUpdateRequest
  - Response DTO/body: MemoryMutationResult
  - Source: MemoryClient.cs

### Repo

- EditFileAsync: POST /mcpserver/repo/edit
  - Request DTO/body: RepoEditRequest
  - Response DTO/body: RepoEditResult
  - Source: RepoClient.cs
- ListAsync: GET /mcpserver/repo/list?path={path}
  - Request DTO/body: none
  - Response DTO/body: RepoListResult
  - Source: RepoClient.cs
- ReadFileAsync: GET /mcpserver/repo/file?path={path}
  - Request DTO/body: none
  - Response DTO/body: RepoFileReadResult
  - Source: RepoClient.cs
- WriteFileAsync: POST /mcpserver/repo/file
  - Request DTO/body: RepoWriteRequest
  - Response DTO/body: RepoWriteResult
  - Source: RepoClient.cs

### Requirements

- CopyFrAcceptanceCriteriaFromTodoAsync: POST /mcpserver/requirements/fr/{id}/acceptance-criteria/copy-from-todo
  - Request DTO/body: CopyAcceptanceCriteriaFromTodoRequest
  - Response DTO/body: FrEntry
  - Source: RequirementsClient.cs
- CopyTestAcceptanceCriteriaFromTodoAsync: POST /mcpserver/requirements/test/{id}/acceptance-criteria/copy-from-todo
  - Request DTO/body: CopyAcceptanceCriteriaFromTodoRequest
  - Response DTO/body: TestEntry
  - Source: RequirementsClient.cs
- CopyTrAcceptanceCriteriaFromTodoAsync: POST /mcpserver/requirements/tr/{id}/acceptance-criteria/copy-from-todo
  - Request DTO/body: CopyAcceptanceCriteriaFromTodoRequest
  - Response DTO/body: TrEntry
  - Source: RequirementsClient.cs
- GenerateAsync: GET /mcpserver/requirements/generate?doc={doc}&format={format}
  - Request DTO/body: none
  - Response DTO/body: RequirementsGeneratedDocument
  - Source: RequirementsClient.cs
- CreateBatchAsync: POST /mcpserver/requirements/batch
  - Request DTO/body: CreateRequirementsBatchRequest
  - Response DTO/body: RequirementsBatchResult
  - Source: RequirementsClient.cs
- CreateFrAsync: POST /mcpserver/requirements/fr
  - Request DTO/body: CreateFrRequest
  - Response DTO/body: FrEntry
  - Source: RequirementsClient.cs
- CreateFrBatchAsync: POST /mcpserver/requirements/fr/batch
  - Request DTO/body: CreateFrBatchRequest
  - Response DTO/body: RequirementsBatchResult
  - Source: RequirementsClient.cs
- CreateTestAsync: POST /mcpserver/requirements/test
  - Request DTO/body: CreateTestRequest
  - Response DTO/body: TestEntry
  - Source: RequirementsClient.cs
- CreateTestBatchAsync: POST /mcpserver/requirements/test/batch
  - Request DTO/body: CreateTestBatchRequest
  - Response DTO/body: RequirementsBatchResult
  - Source: RequirementsClient.cs
- CreateTrAsync: POST /mcpserver/requirements/tr
  - Request DTO/body: CreateTrRequest
  - Response DTO/body: TrEntry
  - Source: RequirementsClient.cs
- CreateTrBatchAsync: POST /mcpserver/requirements/tr/batch
  - Request DTO/body: CreateTrBatchRequest
  - Response DTO/body: RequirementsBatchResult
  - Source: RequirementsClient.cs
- DeleteFrAsync: DELETE /mcpserver/requirements/fr/{id}
  - Request DTO/body: none
  - Response DTO/body: RequirementsMutationResult
  - Source: RequirementsClient.cs
- DeleteMappingAsync: DELETE /mcpserver/requirements/mapping/{frId}
  - Request DTO/body: none
  - Response DTO/body: RequirementsMutationResult
  - Source: RequirementsClient.cs
- DeleteTestAsync: DELETE /mcpserver/requirements/test/{id}
  - Request DTO/body: none
  - Response DTO/body: RequirementsMutationResult
  - Source: RequirementsClient.cs
- DeleteTrAsync: DELETE /mcpserver/requirements/tr/{id}
  - Request DTO/body: none
  - Response DTO/body: RequirementsMutationResult
  - Source: RequirementsClient.cs
- GetFrAsync: GET /mcpserver/requirements/fr/{id}
  - Request DTO/body: none
  - Response DTO/body: FrEntry
  - Source: RequirementsClient.cs
- GetMappingAsync: GET /mcpserver/requirements/mapping/{frId}
  - Request DTO/body: none
  - Response DTO/body: FrTrMapping
  - Source: RequirementsClient.cs
- GetTestAsync: GET /mcpserver/requirements/test/{id}
  - Request DTO/body: none
  - Response DTO/body: TestEntry
  - Source: RequirementsClient.cs
- GetTrAsync: GET /mcpserver/requirements/tr/{id}
  - Request DTO/body: none
  - Response DTO/body: TrEntry
  - Source: RequirementsClient.cs
- IngestAsync: POST /mcpserver/requirements/ingest
  - Request DTO/body: RequirementsIngestRequest?
  - Response DTO/body: RequirementsIngestResult
  - Source: RequirementsClient.cs
- RepairFrPlaceholdersAsync: POST /mcpserver/requirements/fr/repair
  - Request DTO/body: none
  - Response DTO/body: object
  - Source: RequirementsClient.cs
- UpdateBatchAsync: PUT /mcpserver/requirements/batch
  - Request DTO/body: UpdateRequirementsBatchRequest
  - Response DTO/body: RequirementsBatchResult
  - Source: RequirementsClient.cs
- UpdateFrAsync: PUT /mcpserver/requirements/fr/{id}
  - Request DTO/body: UpdateFrRequest
  - Response DTO/body: FrEntry
  - Source: RequirementsClient.cs
- UpdateFrBatchAsync: PUT /mcpserver/requirements/fr/batch
  - Request DTO/body: UpdateFrBatchRequest
  - Response DTO/body: RequirementsBatchResult
  - Source: RequirementsClient.cs
- UpdateTestAsync: PUT /mcpserver/requirements/test/{id}
  - Request DTO/body: UpdateTestRequest
  - Response DTO/body: TestEntry
  - Source: RequirementsClient.cs
- UpdateTestBatchAsync: PUT /mcpserver/requirements/test/batch
  - Request DTO/body: UpdateTestBatchRequest
  - Response DTO/body: RequirementsBatchResult
  - Source: RequirementsClient.cs
- UpdateTrAsync: PUT /mcpserver/requirements/tr/{id}
  - Request DTO/body: UpdateTrRequest
  - Response DTO/body: TrEntry
  - Source: RequirementsClient.cs
- UpdateTrBatchAsync: PUT /mcpserver/requirements/tr/batch
  - Request DTO/body: UpdateTrBatchRequest
  - Response DTO/body: RequirementsBatchResult
  - Source: RequirementsClient.cs
- UpsertMappingAsync: PUT /mcpserver/requirements/mapping/{frId}
  - Request DTO/body: UpsertFrTrMappingRequest
  - Response DTO/body: FrTrMapping
  - Source: RequirementsClient.cs

### SessionLog

- AppendDialogAsync: POST /mcpserver/sessionlog/{agent}/{sessionId}/{requestId}/dialog
  - Request DTO/body: List<ProcessingDialogItemDto>
  - Response DTO/body: DialogAppendResult
  - Source: SessionLogClient.cs
- BeginTurnAsync: POST /mcpserver/sessionlog/{agent}/{sessionId}/{requestId}/begin
  - Request DTO/body: anonymous object
  - Response DTO/body: SessionLogTurnSubmitResult
  - Source: SessionLogClient.cs
- ClearTurnSectionAsync: DELETE /mcpserver/sessionlog/{agent}/{sessionId}/{requestId}/sections/{section}
  - Request DTO/body: none
  - Response DTO/body: SessionLogMutationResult
  - Source: SessionLogClient.cs
- CompleteTurnAsync: POST /mcpserver/sessionlog/{agent}/{sessionId}/{requestId}/complete
  - Request DTO/body: UnifiedRequestEntryDto
  - Response DTO/body: SessionLogTurnSubmitResult
  - Source: SessionLogClient.cs
- DeleteSessionAsync: DELETE /mcpserver/sessionlog/{agent}/{sessionId}
  - Request DTO/body: none
  - Response DTO/body: SessionLogMutationResult
  - Source: SessionLogClient.cs
- DeleteTurnAsync: DELETE /mcpserver/sessionlog/{agent}/{sessionId}/{requestId}
  - Request DTO/body: none
  - Response DTO/body: SessionLogMutationResult
  - Source: SessionLogClient.cs
- DeleteTurnItemAsync: DELETE /mcpserver/sessionlog/{agent}/{sessionId}/{requestId}/sections/{section}/items/{itemKey}
  - Request DTO/body: none
  - Response DTO/body: SessionLogMutationResult
  - Source: SessionLogClient.cs
- FailTurnAsync: POST /mcpserver/sessionlog/{agent}/{sessionId}/{requestId}/fail
  - Request DTO/body: UnifiedRequestEntryDto
  - Response DTO/body: SessionLogTurnSubmitResult
  - Source: SessionLogClient.cs
- OpenSessionAsync: POST /mcpserver/sessionlog/{agent}/{sessionId}/open
  - Request DTO/body: anonymous object
  - Response DTO/body: SessionLifecycleOpenResult
  - Source: SessionLogClient.cs
- PatchTurnAsync: PATCH /mcpserver/sessionlog/{agent}/{sessionId}/{requestId}
  - Request DTO/body: UnifiedRequestEntryDto
  - Response DTO/body: SessionLogTurnSubmitResult
  - Source: SessionLogClient.cs
- QueryAsync: GET /mcpserver/sessionlog?agent={agent}&agentDefinitionId={agentDefinitionId}&model={model}&text={text}&from={from}&to={to}&limit={limit}&offset={offset}
  - Request DTO/body: none
  - Response DTO/body: SessionLogQueryResult
  - Source: SessionLogClient.cs
- RepairWorkspaceStampsAsync: POST /mcpserver/sessionlog/repair-workspace-stamps?dryRun={dryRun}
  - Request DTO/body: none
  - Response DTO/body: SessionLogWorkspaceStampRepairResult
  - Source: SessionLogClient.cs
- ReplaceTurnAsync: PUT /mcpserver/sessionlog/{agent}/{sessionId}/{requestId}
  - Request DTO/body: UnifiedRequestEntryDto
  - Response DTO/body: SessionLogMutationResult
  - Source: SessionLogClient.cs
- ReplaceTurnSectionAsync: PUT /mcpserver/sessionlog/{agent}/{sessionId}/{requestId}/sections/{section}
  - Request DTO/body: UnifiedRequestEntryDto
  - Response DTO/body: SessionLogMutationResult
  - Source: SessionLogClient.cs
- SubmitAsync: POST /mcpserver/sessionlog
  - Request DTO/body: UnifiedSessionLogDto
  - Response DTO/body: SessionLogSubmitResult
  - Source: SessionLogClient.cs
- UpsertTurnAsync: POST /mcpserver/sessionlog/{agent}/{sessionId}/turn
  - Request DTO/body: UnifiedRequestEntryDto
  - Response DTO/body: SessionLogTurnSubmitResult
  - Source: SessionLogClient.cs

### Subscriber

- AbortTransactionAsync: POST /mcpserver/subscriber/transactions/{transactionId}/abort
  - Request DTO/body: TransactionAbortRequest
  - Response DTO/body: TransactionAbortResponse
  - Source: SubscriberClient.cs
- CommitDiffgramAsync: POST /mcpserver/subscriber/diffgrams/commit
  - Request DTO/body: DiffgramCommitRequest
  - Response DTO/body: DiffgramCommitResponse
  - Source: SubscriberClient.cs
- GetTransactionStatusAsync: GET /mcpserver/subscriber/transactions/{transactionId}/status
  - Request DTO/body: none
  - Response DTO/body: TransactionStatusResponse
  - Source: SubscriberClient.cs

### Template

- CreateAsync: POST /mcpserver/templates
  - Request DTO/body: TemplateCreateRequest
  - Response DTO/body: TemplateMutationResult
  - Source: TemplateClient.cs
- DeleteAsync: DELETE /mcpserver/templates/{id}
  - Request DTO/body: none
  - Response DTO/body: TemplateMutationResult
  - Source: TemplateClient.cs
- GetAsync: GET /mcpserver/templates/{id}
  - Request DTO/body: none
  - Response DTO/body: TemplateItem
  - Source: TemplateClient.cs
- QueryAsync: GET /mcpserver/templates?category={category}&tag={tag}&keyword={keyword}
  - Request DTO/body: none
  - Response DTO/body: TemplateQueryResult
  - Source: TemplateClient.cs
- ResolveAsync: POST /mcpserver/templates/{id}/resolve
  - Request DTO/body: TemplateResolveRequest
  - Response DTO/body: TemplateResolveResult
  - Source: TemplateClient.cs
- TestAsync: POST /mcpserver/templates/{id}/test
  - Request DTO/body: TemplateTestRequest
  - Response DTO/body: TemplateTestResult
  - Source: TemplateClient.cs
- TestInlineAsync: POST /mcpserver/templates/test
  - Request DTO/body: TemplateTestRequest
  - Response DTO/body: TemplateTestResult
  - Source: TemplateClient.cs
- UpdateAsync: PUT /mcpserver/templates/{id}
  - Request DTO/body: TemplateUpdateRequest
  - Response DTO/body: TemplateMutationResult
  - Source: TemplateClient.cs

### Todo

- AdbStepAsync: POST /mcpserver/todo-execution/adb/step
  - Request DTO/body: AdbStepRequest
  - Response DTO/body: AdbStepResult
  - Source: TodoClient.cs
- AnalyzeRequirementsAsync: POST /mcpserver/todo/{id}/requirements
  - Request DTO/body: none
  - Response DTO/body: RequirementsAnalysisResult
  - Source: TodoClient.cs
- AppendCheckpointAsync: POST /mcpserver/todo-execution/todos/{todoId}/checkpoints
  - Request DTO/body: AppendTodoCheckpointRequest
  - Response DTO/body: AppendTodoCheckpointResult
  - Source: TodoClient.cs
- CreateAsync: POST /mcpserver/todo
  - Request DTO/body: TodoCreateRequest
  - Response DTO/body: TodoMutationResult
  - Source: TodoClient.cs
- CloseAsync: POST /mcpserver/todo/{id}/close
  - Request DTO/body: none
  - Response DTO/body: TodoMutationResult
  - Source: TodoClient.cs
- CreateIterationPhaseAsync: POST /mcpserver/todo-execution/phases
  - Request DTO/body: CreateIterationPhaseRequest
  - Response DTO/body: CreateIterationPhaseResult
  - Source: TodoClient.cs
- CreateTodosFromPlanAsync: POST /mcpserver/todo-execution/phases/{phaseId}/todos
  - Request DTO/body: CreateTodosFromPlanRequest
  - Response DTO/body: CreateTodosFromPlanResult
  - Source: TodoClient.cs
- DeleteAsync: DELETE /mcpserver/todo/{id}
  - Request DTO/body: none
  - Response DTO/body: TodoMutationResult
  - Source: TodoClient.cs
- GetActiveTodoAsync: GET /mcpserver/todo-execution/active
  - Request DTO/body: none
  - Response DTO/body: ActiveTodoResult
  - Source: TodoClient.cs
- GetAsync: GET /mcpserver/todo/{id}
  - Request DTO/body: none
  - Response DTO/body: TodoFlatItem
  - Source: TodoClient.cs
- GetAuditAsync: GET /mcpserver/todo/{id}/audit?limit={limit}&offset={offset}
  - Request DTO/body: none
  - Response DTO/body: TodoAuditQueryResult
  - Source: TodoClient.cs
- GetDeltaContextAsync: GET /mcpserver/todo-execution/todos/{todoId}/delta?sinceCheckpointId={sinceCheckpointId}
  - Request DTO/body: none
  - Response DTO/body: TodoDeltaContext
  - Source: TodoClient.cs
- GetExecutionContextAsync: GET /mcpserver/todo-execution/todos/{todoId}?requirementSnippetLimit={requirementSnippetLimit}&sessionTurnSummaryLimit={sessionTurnSummaryLimit}
  - Request DTO/body: none
  - Response DTO/body: ActiveTodoContext
  - Source: TodoClient.cs
- GetNextReadyTodoAsync: GET /mcpserver/todo-execution/next-ready
  - Request DTO/body: none
  - Response DTO/body: ActiveTodoResult
  - Source: TodoClient.cs
- GetProjectionStatusAsync: GET /mcpserver/todo/projection/status
  - Request DTO/body: none
  - Response DTO/body: TodoProjectionStatusResult
  - Source: TodoClient.cs
- LinkSessionTurnsAsync: POST /mcpserver/todo-execution/todos/{todoId}/session-turns
  - Request DTO/body: LinkTodoToSessionTurnsRequest
  - Response DTO/body: LinkTodoToSessionTurnsResult
  - Source: TodoClient.cs
- MoveAsync: POST /mcpserver/todo/{id}/move
  - Request DTO/body: TodoMoveRequest
  - Response DTO/body: TodoMutationResult
  - Source: TodoClient.cs
- QueryAsync: GET /mcpserver/todo?keyword={keyword}&priority={priority}&section={section}&id={id}&done={done}
  - Request DTO/body: none
  - Response DTO/body: TodoQueryResult
  - Source: TodoClient.cs
- QueueImplementPromptAsync: POST /mcpserver/todo/{id}/prompt/implement/queue
  - Request DTO/body: AgentPoolOneShotRequest?
  - Response DTO/body: AgentPoolEnqueueResult
  - Source: TodoClient.cs
- QueuePlanPromptAsync: POST /mcpserver/todo/{id}/prompt/plan/queue
  - Request DTO/body: AgentPoolOneShotRequest?
  - Response DTO/body: AgentPoolEnqueueResult
  - Source: TodoClient.cs
- QueueStatusPromptAsync: POST /mcpserver/todo/{id}/prompt/status/queue
  - Request DTO/body: AgentPoolOneShotRequest?
  - Response DTO/body: AgentPoolEnqueueResult
  - Source: TodoClient.cs
- RecordValidationResultAsync: POST /mcpserver/todo-execution/todos/{todoId}/validation
  - Request DTO/body: RecordTodoValidationResultRequest
  - Response DTO/body: RecordTodoValidationResultResult
  - Source: TodoClient.cs
- RepairProjectionAsync: POST /mcpserver/todo/projection/repair
  - Request DTO/body: none
  - Response DTO/body: TodoProjectionRepairResult
  - Source: TodoClient.cs
- SetTestPlanAsync: PUT /mcpserver/todo-execution/todos/{todoId}/test-plan
  - Request DTO/body: SetTodoTestPlanRequest
  - Response DTO/body: SetTodoTestPlanResult
  - Source: TodoClient.cs
- StreamImplementAsync: GET /mcpserver/todo/{id}/prompt/implement
  - Request DTO/body: none
  - Response DTO/body: SSE stream of string
  - Source: TodoClient.cs
- StreamPlanAsync: GET /mcpserver/todo/{id}/prompt/plan
  - Request DTO/body: none
  - Response DTO/body: SSE stream of string
  - Source: TodoClient.cs
- StreamStatusAsync: GET /mcpserver/todo/{id}/prompt/status
  - Request DTO/body: none
  - Response DTO/body: SSE stream of string
  - Source: TodoClient.cs
- UpdateAsync: PUT /mcpserver/todo/{id}
  - Request DTO/body: TodoUpdateRequest
  - Response DTO/body: TodoMutationResult
  - Source: TodoClient.cs
- UpdateExecutionStatusAsync: POST /mcpserver/todo-execution/todos/{todoId}/status
  - Request DTO/body: UpdateTodoStatusRequest
  - Response DTO/body: UpdateTodoStatusResult
  - Source: TodoClient.cs

### ToolRegistry

- AddBucketAsync: POST /mcpserver/tools/buckets
  - Request DTO/body: BucketAddRequest
  - Response DTO/body: BucketMutationResult
  - Source: ToolRegistryClient.cs
- BrowseBucketAsync: GET /mcpserver/tools/buckets/{name}/browse
  - Request DTO/body: none
  - Response DTO/body: BucketBrowseResult
  - Source: ToolRegistryClient.cs
- CreateAsync: POST /mcpserver/tools
  - Request DTO/body: ToolCreateRequest
  - Response DTO/body: ToolMutationResult
  - Source: ToolRegistryClient.cs
- DeleteAsync: DELETE /mcpserver/tools/{id}
  - Request DTO/body: none
  - Response DTO/body: ToolMutationResult
  - Source: ToolRegistryClient.cs
- DeleteBucketAsync: DELETE /mcpserver/tools/buckets/{name}?uninstallTools={uninstallTools}
  - Request DTO/body: none
  - Response DTO/body: BucketMutationResult
  - Source: ToolRegistryClient.cs
- GetAsync: GET /mcpserver/tools/{id}
  - Request DTO/body: none
  - Response DTO/body: ToolDto
  - Source: ToolRegistryClient.cs
- InstallFromBucketAsync: POST /mcpserver/tools/buckets/{bucketName}/install?toolName={toolName}&workspace={workspace}
  - Request DTO/body: none
  - Response DTO/body: ToolMutationResult
  - Source: ToolRegistryClient.cs
- ListAsync: GET /mcpserver/tools?workspace={workspace}
  - Request DTO/body: none
  - Response DTO/body: ToolSearchResult
  - Source: ToolRegistryClient.cs
- ListBucketsAsync: GET /mcpserver/tools/buckets
  - Request DTO/body: none
  - Response DTO/body: BucketListResult
  - Source: ToolRegistryClient.cs
- SearchAsync: GET /mcpserver/tools/search?keyword={keyword}&workspace={workspace}
  - Request DTO/body: none
  - Response DTO/body: ToolSearchResult
  - Source: ToolRegistryClient.cs
- SyncBucketAsync: POST /mcpserver/tools/buckets/{name}/sync
  - Request DTO/body: none
  - Response DTO/body: BucketSyncResult
  - Source: ToolRegistryClient.cs
- UpdateAsync: PUT /mcpserver/tools/{id}
  - Request DTO/body: ToolUpdateRequest
  - Response DTO/body: ToolMutationResult
  - Source: ToolRegistryClient.cs

### Agent Help

- CreateSessionAsync: POST /mcpserver/agent-help/session
  - Request DTO/body: AgentHelpSessionCreateRequest
  - Response DTO/body: AgentHelpSessionCreateResponse
  - Source: AgentHelpClient.cs
- GetStatusAsync: GET /mcpserver/agent-help/session/{sessionId}
  - Request DTO/body: none
  - Response DTO/body: AgentHelpSessionStatusDto
  - Source: AgentHelpClient.cs
- SubmitTurnAsync: POST /mcpserver/agent-help/session/{sessionId}/turn
  - Request DTO/body: AgentHelpTurnRequest
  - Response DTO/body: AgentHelpTurnResponse
  - Source: AgentHelpClient.cs
- GetTranscriptAsync: GET /mcpserver/agent-help/session/{sessionId}/transcript
  - Request DTO/body: none
  - Response DTO/body: AgentHelpTranscriptResponse
  - Source: AgentHelpClient.cs

### Triage

- FlushGroupAsync: POST /mcpserver/triage/groups/{id}/flush
  - Request DTO/body: none
  - Response DTO/body: TriageGroupDetail
  - Source: TriageClient.cs
- ConsolidateIntoGroupAsync: POST /mcpserver/triage/groups/{id}/consolidate
  - Request DTO/body: TriageGroupSelectionRequest
  - Response DTO/body: TriageGroupEditResult
  - Source: TriageClient.cs
- CreateGroupFromSelectionAsync: POST /mcpserver/triage/groups/new
  - Request DTO/body: TriageGroupSelectionRequest
  - Response DTO/body: TriageGroupEditResult
  - Source: TriageClient.cs
- GetDashboardAsync: GET /mcpserver/triage/dashboard?workspacePath={workspacePath}
  - Request DTO/body: none
  - Response DTO/body: TriageDashboardResult
  - Source: TriageClient.cs
- GetGroupAsync: GET /mcpserver/triage/groups/{id}
  - Request DTO/body: none
  - Response DTO/body: TriageGroupDetail
  - Source: TriageClient.cs
- GetReportAsync: GET /mcpserver/triage/reports/{id}
  - Request DTO/body: none
  - Response DTO/body: TriageReportDetail
  - Source: TriageClient.cs
- GetRunAsync: GET /mcpserver/triage/runs/{id}
  - Request DTO/body: none
  - Response DTO/body: TriageResearchRunDetail
  - Source: TriageClient.cs
- QueryCreatedTodosAsync: GET /mcpserver/triage/todos?workspacePath={workspacePath}
  - Request DTO/body: none
  - Response DTO/body: TriageCreatedTodoQueryResult
  - Source: TriageClient.cs
- QueryGroupsAsync: GET /mcpserver/triage/groups?status={status}&workspacePath={workspacePath}
  - Request DTO/body: none
  - Response DTO/body: TriageGroupQueryResult
  - Source: TriageClient.cs
- QueryRunsAsync: GET /mcpserver/triage/runs?status={status}&groupId={groupId}&workspacePath={workspacePath}
  - Request DTO/body: none
  - Response DTO/body: TriageRunQueryResult
  - Source: TriageClient.cs
- MergeGroupsAsync: POST /mcpserver/triage/groups/{id}/merge
  - Request DTO/body: TriageGroupSelectionRequest
  - Response DTO/body: TriageGroupEditResult
  - Source: TriageClient.cs
- RetryGroupAsync: POST /mcpserver/triage/groups/{id}/retry?force={force}
  - Request DTO/body: none
  - Query parameters include force optional boolean to fail an active processing run before requeueing
  - Response DTO/body: TriageGroupDetail
  - Source: TriageClient.cs
- SubmitReportAsync: POST /mcpserver/triage/reports
  - Request DTO/body: TriageReportRequest
  - Response DTO/body: TriageReportSubmitResult
  - Source: TriageClient.cs

### Tunnel

- DisableAsync: POST /mcpserver/tunnel/{providerName}/disable
  - Request DTO/body: none
  - Response DTO/body: TunnelProviderInfo
  - Source: TunnelClient.cs
- EnableAsync: POST /mcpserver/tunnel/{providerName}/enable
  - Request DTO/body: none
  - Response DTO/body: TunnelProviderInfo
  - Source: TunnelClient.cs
- GetStatusAsync: GET /mcpserver/tunnel/{providerName}/status
  - Request DTO/body: none
  - Response DTO/body: TunnelProviderInfo
  - Source: TunnelClient.cs
- RestartAsync: POST /mcpserver/tunnel/{providerName}/restart
  - Request DTO/body: none
  - Response DTO/body: TunnelProviderInfo
  - Source: TunnelClient.cs
- StartAsync: POST /mcpserver/tunnel/{providerName}/start
  - Request DTO/body: none
  - Response DTO/body: TunnelProviderInfo
  - Source: TunnelClient.cs
- StopAsync: POST /mcpserver/tunnel/{providerName}/stop
  - Request DTO/body: none
  - Response DTO/body: TunnelProviderInfo
  - Source: TunnelClient.cs

### TurnTransactions

- GetStatusAsync: GET /mcpserver/turntransactions/status
  - Request DTO/body: none
  - Response DTO/body: TurnTransactionStatusResponse
  - Source: TurnTransactionsClient.cs
- PurgePubSubRetentionAsync: POST /mcpserver/turntransactions/pubsub/retention/purge?completedBeforeUtc={completedBeforeUtc}&maxMessages={maxMessages}
  - Request DTO/body: none
  - Response DTO/body: TransactionPubSubRetentionResult
  - Source: TurnTransactionsClient.cs
- ReplayPubSubAsync: POST /mcpserver/turntransactions/pubsub/replay?maxMessages={maxMessages}
  - Request DTO/body: none
  - Response DTO/body: TransactionPubSubReplayResult
  - Source: TurnTransactionsClient.cs

### Voice

- CreateSessionAsync: POST /mcpserver/voice/session
  - Request DTO/body: VoiceSessionCreateRequest
  - Response DTO/body: VoiceSessionCreateResponse
  - Source: VoiceClient.cs
- DeleteSessionAsync: DELETE /mcpserver/voice/session/{sessionId}
  - Request DTO/body: none
  - Response DTO/body: Task<bool>
  - Source: VoiceClient.cs
- EscapeAsync: POST /mcpserver/voice/session/{sessionId}/escape
  - Request DTO/body: none
  - Response DTO/body: VoiceEscapeResponse
  - Source: VoiceClient.cs
- FindSessionByDeviceAsync: GET /mcpserver/voice/session?deviceId={deviceId}
  - Request DTO/body: none
  - Response DTO/body: VoiceSessionStatus
  - Source: VoiceClient.cs
- GetStatusAsync: GET /mcpserver/voice/session/{sessionId}
  - Request DTO/body: none
  - Response DTO/body: VoiceSessionStatus
  - Source: VoiceClient.cs
- GetTranscriptAsync: GET /mcpserver/voice/session/{sessionId}/transcript
  - Request DTO/body: none
  - Response DTO/body: VoiceTranscriptResponse
  - Source: VoiceClient.cs
- InterruptAsync: POST /mcpserver/voice/session/{sessionId}/interrupt
  - Request DTO/body: none
  - Response DTO/body: VoiceInterruptResponse
  - Source: VoiceClient.cs
- SubmitTurnAsync: POST /mcpserver/voice/session/{sessionId}/turn
  - Request DTO/body: VoiceTurnRequest
  - Response DTO/body: VoiceTurnResponse
  - Source: VoiceClient.cs
- SubmitTurnStreamingAsync: POST /mcpserver/voice/session/{sessionId}/turn/stream
  - Request DTO/body: VoiceTurnRequest
  - Response DTO/body: SSE stream of VoiceTurnStreamEvent
  - Source: VoiceClient.cs

### Workspace

- ApplyPolicyAsync: POST /mcpserver/workspace/policy
  - Request DTO/body: WorkspacePolicyApplyRequest
  - Response DTO/body: WorkspacePolicyApplyResult
  - Source: WorkspaceClient.cs
- CreateAsync: POST /mcpserver/workspace
  - Request DTO/body: WorkspaceCreateRequest
  - Response DTO/body: WorkspaceMutationResult
  - Source: WorkspaceClient.cs
- DeleteAsync: DELETE /mcpserver/workspace/{key}
  - Request DTO/body: none
  - Response DTO/body: WorkspaceMutationResult
  - Source: WorkspaceClient.cs
- GetAsync: GET /mcpserver/workspace/{key}
  - Request DTO/body: none
  - Response DTO/body: WorkspaceDto
  - Source: WorkspaceClient.cs
- GetGlobalPromptAsync: GET /mcpserver/workspace/prompt
  - Request DTO/body: none
  - Response DTO/body: GlobalPromptResult
  - Source: WorkspaceClient.cs
- GetStatusAsync: GET /mcpserver/workspace/{key}/status
  - Request DTO/body: none
  - Response DTO/body: WorkspaceProcessStatus
  - Source: WorkspaceClient.cs
- InitAsync: POST /mcpserver/workspace/{key}/init
  - Request DTO/body: none
  - Response DTO/body: WorkspaceInitResult
  - Source: WorkspaceClient.cs
- ListAsync: GET /mcpserver/workspace
  - Request DTO/body: none
  - Response DTO/body: WorkspaceListResult
  - Source: WorkspaceClient.cs
- RegenerateMarkersAsync: POST /mcpserver/workspace/markers/regenerate
  - Request DTO/body: none
  - Response DTO/body: MarkerRegenerationResult
  - Source: WorkspaceClient.cs
- StartAsync: POST /mcpserver/workspace/{key}/start
  - Request DTO/body: none
  - Response DTO/body: WorkspaceProcessStatus
  - Source: WorkspaceClient.cs
- StopAsync: POST /mcpserver/workspace/{key}/stop
  - Request DTO/body: none
  - Response DTO/body: WorkspaceProcessStatus
  - Source: WorkspaceClient.cs
- UpdateAsync: PUT /mcpserver/workspace/{key}
  - Request DTO/body: WorkspaceUpdateRequest
  - Response DTO/body: WorkspaceMutationResult
  - Source: WorkspaceClient.cs
- UpdateGlobalPromptAsync: PUT /mcpserver/workspace/prompt
  - Request DTO/body: GlobalPromptUpdateRequest
  - Response DTO/body: GlobalPromptResult
  - Source: WorkspaceClient.cs

## DTO Object Reference

### ContextClient.cs

#### RebuildIndexResult (class)

- status: string?

#### ContextSourcesResult (class)

- sources: IReadOnlyList<ContextSource>
Field names below are the exact JSON property names declared with [JsonPropertyName]. Types are the public C# property types in the client package. DTOs with no listed fields either have no JSON properties in the client model or are enums.

### AgentModels.cs

#### AgentSeedDefaultsResult (class)

- seeded: int

#### AgentDefinition (class)

- id: string
- displayName: string
- defaultLaunchCommand: string
- defaultInstructionFile: string
- defaultModels: IReadOnlyList<string>
- defaultBranchStrategy: string
- defaultSeedPrompt: string
- isBuiltIn: bool
- createdAt: DateTime
- modifiedAt: DateTime

#### AgentDefinitionRequest (class)

- id: string
- displayName: string
- defaultLaunchCommand: string
- defaultInstructionFile: string
- defaultModels: IReadOnlyList<string>
- defaultBranchStrategy: string
- defaultSeedPrompt: string

#### AgentDefinitionListResult (class)

- items: IReadOnlyList<AgentDefinition>
- totalCount: int

#### AgentWorkspaceConfig (class)

- id: int
- agentId: string
- workspacePath: string
- enabled: bool
- banned: bool
- bannedReason: string?
- bannedUntilPr: int?
- agentIsolation: string
- launchCommandOverride: string?
- modelsOverride: IReadOnlyList<string>?
- branchStrategyOverride: string?
- seedPromptOverride: string?
- markerAdditions: string
- instructionFilesOverride: IReadOnlyList<string>?
- addedAt: DateTime
- lastLaunchedAt: DateTime?

#### AgentWorkspaceRequest (class)

- agentId: string
- enabled: bool
- agentIsolation: string
- launchCommandOverride: string?
- modelsOverride: IReadOnlyList<string>?
- branchStrategyOverride: string?
- seedPromptOverride: string?
- markerAdditions: string
- instructionFilesOverride: IReadOnlyList<string>?

#### AgentWorkspaceListResult (class)

- items: IReadOnlyList<AgentWorkspaceConfig>
- totalCount: int

#### AgentBanRequest (class)

- reason: string?
- bannedUntilPr: int?
- global: bool

#### AgentEventRequest (class)

- agentId: string
- eventType: int
- details: string?

#### AgentEvent (class)

- id: long
- agentId: string
- workspacePath: string
- eventType: int
- userId: string?
- details: string?
- timestamp: DateTime

#### AgentEventListResult (class)

- items: IReadOnlyList<AgentEvent>
- totalCount: int

#### AgentMutationResult (class)

- success: bool
- error: string?

#### AgentValidateResult (class)

- valid: bool
- error: string?
- path: string?

#### AgentProcessStatus (enum)

- Starting
- Running
- Stopped
- Failed

#### AgentProcessInfo (class)

- processId: int?
- agentId: string
- workspacePath: string
- startedAt: DateTime
- status: AgentProcessStatus
- exitCode: int?
- workDirectory: string?
- errorMessage: string?

#### AgentRunningListResult (class)

- agents: IReadOnlyList<AgentProcessInfo>

### AgentPoolModels.cs

#### AgentPoolOneShotContext (enum)

- Plan
- Status
- Implement
- AdHoc

#### AgentPoolOneShotRequest (class)

- agentName: string?
- workspacePath: string?
- context: AgentPoolOneShotContext?
- promptTemplateId: string?
- promptText: string?
- id: string?
- values: Dictionary<string, object?>?
- useWorkspaceContext: bool

#### AgentPoolAgentStatus (class)

- agentName: string
- workspacePath: string?
- lifecycle: string
- sessionId: string?
- activeJobId: string?
- lastRequestPrompt: string?
- activeVoiceLinks: int
- readOnlySubscribers: int
- isInteractiveDefault: bool
- isTodoPlanDefault: bool
- isTodoStatusDefault: bool
- isTodoImplementDefault: bool

#### AgentPoolQueueItem (class)

- jobId: string
- agentName: string?
- workspacePath: string?
- status: string
- context: AgentPoolOneShotContext?
- promptTemplateId: string?
- renderedPrompt: string?
- responseText: string?
- error: string?
- createdUtc: DateTimeOffset
- startedUtc: DateTimeOffset?
- completedUtc: DateTimeOffset?
- sessionId: string?

#### AgentPoolMutationResult (class)

- success: bool
- error: string?

#### AgentPoolEnqueueResult (class)

- jobId: string?
- agentName: string?
- renderedPrompt: string?

#### AgentPoolConnectResult (class)

- agentName: string?
- sessionId: string?

#### AgentPoolPromptResolutionResult (class)

- promptText: string?
- templateId: string?
- templateResolved: bool

#### AgentPoolNotificationEvent (class)

- eventType: string
- agentName: string?
- workspacePath: string?
- jobId: string?
- sessionId: string?
- lastRequestPrompt: string?
- timestampUtc: DateTimeOffset
- message: string?

#### AgentPoolJobStreamEvent (class)

- jobId: string
- eventType: string
- status: string?
- text: string?
- error: string?
- timestampUtc: DateTimeOffset

### AuthConfigModels.cs

#### AuthConfigResponse (class)

- enabled: bool
- authority: string?
- clientId: string?
- scopes: string?
- deviceAuthorizationEndpoint: string?
- tokenEndpoint: string?

#### AuthDeviceAuthorizationRequest (class)

- No [JsonPropertyName] properties declared in this client DTO.

#### AuthDeviceAuthorizationResponse (class)

- device_code: string?
- user_code: string?
- verification_uri: string?
- verification_uri_complete: string?
- expires_in: int?
- interval: int?
- error: string?
- error_description: string?

#### AuthTokenRequest (class)

- No [JsonPropertyName] properties declared in this client DTO.

#### AuthTokenResponse (class)

- access_token: string?
- token_type: string?
- expires_in: int?
- refresh_expires_in: int?
- refresh_token: string?
- scope: string?
- id_token: string?
- error: string?
- error_description: string?

### BrainSlotModels.cs

#### BrainSlotDto (class)

- slotId: string
- role: string
- displayName: string?
- providerKind: string
- modelId: string
- endpoint: string?
- credentialReference: string
- partyId: string
- enabled: bool
- timeoutSeconds: int
- maxOutputTokens: int
- systemPrompt: string?
- orchestrationWeight: double
- weightVersion: int
- weightUpdatedAtUtc: DateTimeOffset?
- createdAtUtc: DateTimeOffset
- updatedAtUtc: DateTimeOffset

#### UpsertBrainSlotRequest (class)

- role: string
- displayName: string?
- providerKind: string
- modelId: string
- endpoint: string?
- credentialReference: string
- partyId: string
- enabled: bool
- timeoutSeconds: int
- maxOutputTokens: int
- systemPrompt: string?
- orchestrationWeight: double
- replaceExisting: bool

#### BrainSlotStatusResponse (class)

- quadReady: bool
- roleReadiness: IReadOnlyDictionary<string, bool>
- missingRoles: IReadOnlyList<string>
- disabledRoles: IReadOnlyList<string>
- validationErrors: IReadOnlyList<string>

#### BrainSlotInvokeRequest (class)

- input: string
- turnId: string?
- admitToGraphRag: bool
- metadata: IReadOnlyDictionary<string, string>

#### BrainSlotInvokeResponse (class)

- status: string
- reason: string
- slotId: string
- role: string
- transactionId: string?
- diffgramId: string?
- modelId: string?
- output: string?
- startedAtUtc: DateTimeOffset
- completedAtUtc: DateTimeOffset

#### QuadBrainOrchestrationRequest (class)

- input: string
- turnId: string?
- admitCuriosityToGraphRag: bool
- metadata: IReadOnlyDictionary<string, string>
- weightUpdate: QuadBrainWeightUpdateRequest?

#### AotReconciliationRequest (class)

- input: string
- creativityOutput: string
- logicOutput: string
- curiosityOutput: string
- turnId: string?
- metadata: IReadOnlyDictionary<string, string>

#### QuadBrainRoleResult (class)

- role: string
- slotId: string
- status: string
- reason: string
- modelId: string?
- transactionId: string?
- diffgramId: string?
- output: string?
- orchestrationWeight: double
- weightVersion: int

#### AotReconciliationResponse (class)

- status: string
- reason: string
- transactionId: string?
- diffgramId: string?
- slotId: string
- modelId: string?
- output: string?
- startedAtUtc: DateTimeOffset
- completedAtUtc: DateTimeOffset

#### QuadBrainOrchestrationResponse (class)

- status: string
- reason: string
- output: string?
- transactionId: string?
- diffgramId: string?
- roleResults: IReadOnlyList<QuadBrainRoleResult>
- weightUpdate: QuadBrainWeightUpdateResponse?
- startedAtUtc: DateTimeOffset
- completedAtUtc: DateTimeOffset

#### QuadBrainWeightUpdateRequest (class)

- roleWeights: IReadOnlyDictionary<string, double>
- expectedVersions: IReadOnlyDictionary<string, int>
- turnId: string?
- proposedBy: string?
- reasonText: string
- aotApproved: bool
- adminApproved: bool
- safetyGatesPassed: bool
- metadata: IReadOnlyDictionary<string, string>

#### QuadBrainWeightSnapshot (class)

- role: string
- slotId: string
- previousWeight: double
- newWeight: double
- previousVersion: int
- newVersion: int

#### QuadBrainWeightUpdateResponse (class)

- status: string
- reason: string
- transactionId: string?
- diffgramId: string?
- snapshots: IReadOnlyList<QuadBrainWeightSnapshot>
- startedAtUtc: DateTimeOffset
- completedAtUtc: DateTimeOffset

### ContextModels.cs

#### ContextSearchRequest (class)

- query: string?
- sourceType: string?
- limit: int

#### ContextSearchResult (class)

- query: string?
- chunks: IReadOnlyList<ContextChunkResult>
- sourceKeys: IReadOnlyList<string>

#### ContextChunkResult (class)

- id: string
- documentId: string
- content: string
- tokenCount: int
- chunkIndex: int
- score: double

#### ContextPackRequest (class)

- queryId: string?
- query: string?
- limit: int

#### ContextPack (class)

- queryId: string
- chunks: IReadOnlyList<ContextChunkResult>
- sourceKeys: IReadOnlyList<string>

#### ContextSource (class)

- sourceKey: string
- sourceType: string
- ingestedAt: string?

#### WebsiteIngestRequest (class)

- url: string
- includeSubpages: bool
- maxPages: int
- maxDepth: int
- maxBytesPerPage: int
- forceRefresh: bool
- triggerGraphRagIndex: bool

#### WebsiteIngestUrlResult (class)

- url: string
- status: string
- sourceKey: string?
- message: string?
- chunksWritten: int

#### WebsiteIngestResult (class)

- runId: string
- startedAtUtc: string?
- completedAtUtc: string?
- status: string
- documentsIngested: int
- chunksWritten: int
- urlResults: IReadOnlyList<WebsiteIngestUrlResult>
- graphRagIndexed: bool
- graphRagIndexError: string?

#### GraphRagQueryRequest (class)

- query: string
- mode: string?
- maxChunks: int?
- includeContextChunks: bool
- maxEntities: int?
- maxRelationships: int?
- communityDepth: int?
- responseTokenBudget: int?

#### GraphRagIndexRequest (class)

- force: bool

#### GraphRagStatusResult (class)

- enabled: bool
- workspacePath: string
- graphRoot: string
- state: string
- isInitialized: bool
- isIndexed: bool
- lastIndexedAtUtc: string?
- lastSuccessAtUtc: string?
- lastFailureAtUtc: string?
- activeJobId: string?
- failureCode: string?
- lastError: string?
- artifactVersion: string
- lastIndexDurationMs: long?
- lastIndexedDocumentCount: int?
- backend: string
- indexCorpus: string
- queryCorpus: string
- inputPath: string
- inputDocumentCount: int
- visibilityNote: string?

#### GraphRagCitation (class)

- sourceKey: string
- chunkId: string?
- snippet: string?

#### GraphRagQueryResult (class)

- query: string
- mode: string
- answer: string
- citations: IReadOnlyList<GraphRagCitation>
- chunks: IReadOnlyList<ContextChunkResult>
- sourceKeys: IReadOnlyList<string>
- entities: IReadOnlyList<string>
- relationships: IReadOnlyList<string>
- communities: IReadOnlyList<string>
- fallbackUsed: bool
- fallbackReason: string?
- failureCode: string?
- backend: string
- queryCorpus: string
- visibilityNote: string?

#### GraphRagIngestTextRequest (class)

- content: string
- title: string?
- sourceType: string?
- sourceKey: string?
- triggerReindex: bool

#### GraphRagIngestTextResult (class)

- documentId: string
- chunkCount: int
- tokenCount: int
- sourceType: string
- sourceKey: string
- reindexTriggered: bool

#### GraphRagDocumentSummary (class)

- id: string
- sourceType: string
- sourceKey: string
- ingestedAt: string?
- contentHash: string
- chunkCount: int
- totalTokens: int

#### GraphRagDocumentListResult (class)

- documents: IReadOnlyList<GraphRagDocumentSummary>
- totalCount: int

#### GraphRagDocumentChunksResult (class)

- documentId: string
- chunks: IReadOnlyList<GraphRagDocumentChunkItem>
- totalChunks: int

#### GraphRagDocumentChunkItem (class)

- id: string
- content: string
- tokenCount: int
- chunkIndex: int

#### GraphRagDocumentDeleteResult (class)

- documentId: string
- chunksRemoved: int
- success: bool

#### GraphEntityRequest (class)

- name: string
- entityType: string
- description: string?
- metadata: string?

#### GraphEntityResult (class)

- id: string
- name: string
- entityType: string
- description: string?
- metadata: string?
- createdAtUtc: string?
- updatedAtUtc: string?

#### GraphEntityListResult (class)

- entities: IReadOnlyList<GraphEntityResult>
- totalCount: int

#### GraphRelationshipRequest (class)

- sourceEntityId: string
- targetEntityId: string
- relationshipType: string
- description: string?
- weight: double
- metadata: string?

#### GraphRelationshipResult (class)

- id: string
- sourceEntityId: string
- targetEntityId: string
- relationshipType: string
- description: string?
- weight: double
- metadata: string?
- createdAtUtc: string?
- updatedAtUtc: string?

#### GraphRelationshipListResult (class)

- relationships: IReadOnlyList<GraphRelationshipResult>
- totalCount: int

### DesktopModels.cs

#### DesktopLaunchRequest (class)

- executablePath: string
- arguments: string?
- workingDirectory: string?
- environmentVariables: Dictionary<string, string>?
- createNoWindow: bool
- windowStyle: string
- waitForExit: bool
- timeoutMs: int?

#### DesktopLaunchResult (class)

- success: bool
- processId: int?
- exitCode: int?
- errorMessage: string?
- errorCode: int?

### DiagnosticModels.cs

#### DiagnosticExecutionPathResult (class)

- processPath: string?
- baseDirectory: string?

#### DiagnosticAppSettingsPathResult (class)

- environmentName: string?
- contentRootPath: string?
- files: IReadOnlyList<DiagnosticPathFileEntry>

#### DiagnosticPathFileEntry (class)

- path: string?
- exists: bool

### EventStreamModels.cs

#### ChangeEvent (class)

- category: string
- action: string
- entityId: string?
- resourceUri: string?
- timestamp: DateTimeOffset

### FederationModels.cs

#### FederationStatusResponse (class)

- enabled: bool
- role: string
- configuredRole: string
- hubBaseUrl: string?
- proxyId: string?
- hasEnrollmentToken: bool
- targets: IReadOnlyList<FederationTargetInfo>
- workspaceRoutes: IReadOnlyList<WorkspaceRouteInfo>
- proxyCount: int
- hostedWorkspaceCount: int
- queueDepth: int
- conflictCount: int
- fanoutDepth: int
- staleReadStatus: string

#### FederationTargetInfo (class)

- name: string
- baseUrl: string
- hasApiKey: bool
- isDefault: bool

#### WorkspaceRouteInfo (class)

- workspacePath: string
- targetName: string

#### FederationProxyInfo (class)

- proxyId: string
- displayName: string?
- role: string
- baseUrl: string?
- status: string
- lastHeartbeatUtc: DateTimeOffset?
- workspaceCount: int

#### FederationEnrollmentRequest (class)

- proxyId: string?
- displayName: string?
- baseUrl: string?
- enrollmentToken: string?
- metadataJson: string?
- workspaces: IReadOnlyList<FederationWorkspaceRegistrationRequest>

#### FederationEnrollmentResponse (class)

- proxyId: string
- accepted: bool
- serverTimeUtc: DateTimeOffset
- heartbeatSeconds: int

#### FederationHeartbeatRequest (class)

- status: string?
- metadataJson: string?
- workspaces: IReadOnlyList<FederationWorkspaceRegistrationRequest>

#### FederationHeartbeatResponse (class)

- proxyId: string
- recordedAtUtc: DateTimeOffset
- queueDepth: int
- conflictCount: int

#### FederationWorkspaceRegistrationRequest (class)

- globalWorkspaceId: string?
- workspaceName: string?
- workspacePath: string
- isEnabled: bool
- version: string?
- metadataJson: string?

#### FederationWorkspaceInfo (class)

- globalWorkspaceId: string
- proxyId: string
- workspaceName: string?
- workspacePath: string
- isEnabled: bool
- version: string?
- lastSeenUtc: DateTimeOffset

#### FederationQueueStatusResponse (class)

- proxyId: string?
- queueDepth: int
- conflictCount: int
- fanoutDepth: int

#### FederationConflictInfo (class)

- conflictId: string
- operationId: string
- proxyId: string
- domain: string
- resourceId: string?
- proxyVersion: string?
- hubVersion: string?
- resolutionStatus: string
- createdAtUtc: DateTimeOffset

#### FederationStateAdapterCoverage (class)

- domain: string
- covered: bool
- localOnly: bool
- applySupported: bool

#### FederationSyncItem (class)

- sequence: long
- operationId: string
- proxyId: string
- sourceOperationId: string?
- globalWorkspaceId: string?
- domain: string
- resourceId: string?
- httpMethod: string?
- path: string?
- method: string?
- headersJson: string?
- bodyBase64: string?
- baseVersion: string?
- hubVersion: string?
- envelope: FederationExecutionEnvelope?

#### FederationExecutionEnvelope (class)

- schemaVersion: int
- envelopeId: string
- sourceProxyId: string
- targetProxyId: string?
- operation: FederationOperationRequest
- issuedAtUtc: DateTimeOffset
- expiresAtUtc: DateTimeOffset
- nonce: string
- bodySha256: string
- applyMode: string
- signature: FederationEnvelopeSignature?

#### FederationLocalExecutionRequest (class)

- method: string
- workspacePath: string?
- executablePath: string?
- arguments: string?
- workingDirectory: string?
- environmentVariables: Dictionary<string, string>?
- createNoWindow: bool
- windowStyle: string
- waitForExit: bool
- timeoutMs: int?

#### FederationLocalExecutionResult (class)

- success: bool
- message: string?
- processId: int?
- exitCode: int?

#### FederationOperationRequest (class)

- operationId: string?
- proxyId: string
- sourceOperationId: string?
- globalWorkspaceId: string?
- domain: string
- resourceId: string?
- httpMethod: string?
- path: string?
- method: string?
- headersJson: string?
- bodyBase64: string?
- baseVersion: string?

#### FederationOperationAckRequest (class)

- status: string
- hubVersion: string?
- error: string?

#### FederationEnvelopeSignature (class)

- algorithm: string
- canonicalization: string
- value: string

#### FederationOperationResponse (class)

- operationId: string
- status: string
- created: bool

#### FederationConflictResolutionRequest (class)

- resolutionStatus: string

#### FederationSyncAckRequest (class)

- status: string
- hubVersion: string?
- error: string?
- proxyId: string?

#### FederationTargetAddRequest (class)

- name: string
- baseUrl: string
- apiKey: string?

#### WorkspaceRouteRequest (class)

- workspacePath: string
- targetName: string

#### TunnelDiscoveryResult (class)

- discovered: int
- targets: IReadOnlyList<FederationTargetInfo>

#### FederationConnectionInfo (class)

- baseUrl: string
- port: int
- apiKey: string

#### FederationPushRequest (class)

- types: IReadOnlyList<string>?

#### FederationPushResult (class)

- succeeded: int
- failed: int
- errors: IReadOnlyList<string>

### GitHubModels.cs

#### GitHubIssueItem (class)

- number: int
- title: string
- state: string?
- url: string?

#### GitHubIssueDetail (class)

- number: int
- title: string
- body: string?
- state: string?
- url: string?
- labels: IReadOnlyList<GitHubLabel>
- assignees: IReadOnlyList<string>
- milestone: string?
- createdAt: string?
- updatedAt: string?
- closedAt: string?
- author: string?
- comments: IReadOnlyList<GitHubIssueComment>

#### GitHubLabel (class)

- name: string
- color: string?
- description: string?

#### GitHubIssueComment (class)

- author: string?
- body: string?
- createdAt: string?

#### GitHubIssueRequest (class)

- title: string?
- body: string?

#### GitHubIssueUpdateRequest (class)

- title: string?
- body: string?
- addLabels: IReadOnlyList<string>?
- removeLabels: IReadOnlyList<string>?
- addAssignees: IReadOnlyList<string>?
- removeAssignees: IReadOnlyList<string>?
- milestone: string?

#### GitHubCommentRequest (class)

- body: string?

#### GitHubIssueListResult (class)

- issues: IReadOnlyList<GitHubIssueItem>
- error: string?

#### GitHubMutationResult (class)

- success: bool
- url: string?
- errorMessage: string?

#### GitHubCreateIssueResult (class)

- number: int
- url: string?

#### GitHubLabelsResult (class)

- labels: IReadOnlyList<GitHubLabel>?
- error: string?

#### GitHubPullItem (class)

- number: int
- title: string
- state: string?
- url: string?

#### GitHubPullListResult (class)

- pulls: IReadOnlyList<GitHubPullItem>
- error: string?

#### IssueSyncResult (class)

- synced: int
- skipped: int
- failed: int
- errors: IReadOnlyList<string>

#### SingleIssueSyncResult (class)

- success: bool
- url: string?
- todoId: string?

#### GitHubAuthStatusResult (class)

- workspacePath: string
- authMode: string
- hasStoredToken: bool
- tokenUpdatedAtUtc: DateTimeOffset?
- tokenExpiresAtUtc: DateTimeOffset?
- cliFallbackAllowed: bool
- oauthConfigured: bool

#### GitHubAuthTokenUpsertRequest (class)

- accessToken: string
- expiresAtUtc: DateTimeOffset?

#### GitHubOAuthConfigResult (class)

- clientId: string
- redirectUri: string
- scopes: string
- authorizeEndpoint: string
- isConfigured: bool

#### GitHubAuthorizeUrlResult (class)

- authorizeUrl: string

#### GitHubWorkflowRunListResult (class)

- runs: IReadOnlyList<GitHubWorkflowRunItem>
- error: string?

#### GitHubWorkflowRunItem (class)

- runId: long
- workflowName: string?
- displayTitle: string?
- headBranch: string?
- status: string?
- conclusion: string?
- event: string?
- url: string?
- createdAt: string?
- updatedAt: string?

#### GitHubWorkflowRunDetail (class)

- runId: long
- workflowName: string?
- displayTitle: string?
- headBranch: string?
- headSha: string?
- status: string?
- conclusion: string?
- event: string?
- url: string?
- attempt: int?
- createdAt: string?
- updatedAt: string?
- jobs: IReadOnlyList<GitHubWorkflowRunJob>

#### GitHubWorkflowRunJob (class)

- name: string?
- status: string?
- conclusion: string?
- startedAt: string?
- completedAt: string?
- url: string?
- steps: IReadOnlyList<GitHubWorkflowRunJobStep>

#### GitHubWorkflowRunJobStep (class)

- name: string?
- status: string?
- conclusion: string?
- number: int?

#### GitHubOperationResult (class)

- success: bool
- error: string?

### HealthModels.cs

#### HealthCheckResult (class)

- status: string?
- version: string?
- checks: IReadOnlyList<HealthCheckEntry>

#### HealthCheckEntry (class)

- name: string?
- status: string?
- description: string?
- duration: double?

#### ServerStartupResult (class)

- serverStartedAtUtc: DateTimeOffset
- nowUtc: DateTimeOffset
- processId: int
- workspace: string?
- port: int?

#### MarkerFileTimestampResult (class)

- repoPath: string?
- markerPath: string?
- exists: bool
- lastWriteTimeUtc: DateTimeOffset?
- creationTimeUtc: DateTimeOffset?
- length: long?
- error: string?

### MemoryModels.cs

#### MemoryScope (enum)

- Global
- Workspace

#### MemoryAddRequest (class)

- id: string?
- category: string
- scope: MemoryScope
- text: string
- updatedBy: string?

#### MemoryUpdateRequest (class)

- category: string?
- scope: MemoryScope?
- text: string?
- updatedBy: string?

#### MemoryItem (class)

- id: string
- category: string
- scope: MemoryScope
- workspacePath: string?
- text: string
- version: int
- createdAtUtc: DateTimeOffset
- updatedAtUtc: DateTimeOffset
- updatedBy: string?

#### MemoryQueryResult (class)

- items: IReadOnlyList<MemoryItem>
- totalCount: int

#### MemoryMutationFailureKind (enum)

- None
- Validation
- Conflict
- NotFound

#### MemoryMutationResult (class)

- success: bool
- error: string?
- memory: MemoryItem?
- failureKind: MemoryMutationFailureKind

### RepoModels.cs

#### RepoFileReadResult (class)

- path: string
- content: string?
- exists: bool

#### RepoWriteRequest (class)

- path: string?
- content: string?

#### RepoWriteResult (class)

- path: string?
- written: bool

#### RepoEditRequest (class)

- path: string?
- oldString: string?
- newString: string?
- replaceAll: bool
- expectedOccurrences: int?

#### RepoEditResult (class)

- path: string?
- written: bool
- replacements: int
- error: string?

#### RepoListResult (class)

- path: string?
- entries: IReadOnlyList<RepoListEntry>

#### RepoListEntry (class)

- name: string
- isDirectory: bool

### RequirementsModels.cs

#### FrEntry (class)

- id: string
- title: string
- body: string
- workspaceId: string
- priority: string
- status: string
- notes: string?
- acceptanceCriteria: IReadOnlyList<AcceptanceCriterion>?

#### TrEntry (class)

- id: string
- title: string
- body: string
- workspaceId: string
- priority: string
- status: string
- notes: string?
- acceptanceCriteria: IReadOnlyList<AcceptanceCriterion>?

#### TestEntry (class)

- id: string
- condition: string
- title: string
- workspaceId: string
- priority: string
- status: string
- notes: string?
- acceptanceCriteria: IReadOnlyList<AcceptanceCriterion>?

#### FrTrMapping (class)

- frId: string
- trIds: IReadOnlyList<string>
- testIds: IReadOnlyList<string>
- workspaceId: string

#### CreateFrRequest (class)

- id: string
- title: string
- body: string
- priority: string?
- status: string?
- notes: string?
- acceptanceCriteria: IReadOnlyList<AcceptanceCriterion>?

#### UpdateFrRequest (class)

- title: string?
- body: string?
- priority: string?
- status: string?
- notes: string?
- acceptanceCriteria: IReadOnlyList<AcceptanceCriterion>?

#### CreateTrRequest (class)

- id: string
- title: string?
- body: string
- priority: string?
- status: string?
- notes: string?
- acceptanceCriteria: IReadOnlyList<AcceptanceCriterion>?

#### UpdateTrRequest (class)

- title: string?
- body: string?
- priority: string?
- status: string?
- notes: string?
- acceptanceCriteria: IReadOnlyList<AcceptanceCriterion>?

#### CreateTestRequest (class)

- id: string
- condition: string
- title: string?
- priority: string?
- status: string?
- notes: string?
- acceptanceCriteria: IReadOnlyList<AcceptanceCriterion>?

#### UpdateTestRequest (class)

- condition: string?
- title: string?
- priority: string?
- status: string?
- notes: string?
- acceptanceCriteria: IReadOnlyList<AcceptanceCriterion>?

#### CreateFrBatchRequest (class)

- records: IReadOnlyList<CreateFrBatchRecord>

#### CreateFrBatchRecord (class)

- id: string?
- title: string?
- body: string?
- description: string?
- priority: string?
- status: string?
- notes: string?
- acceptanceCriteria: IReadOnlyList<AcceptanceCriterion>?

#### UpdateFrBatchRequest (class)

- records: IReadOnlyList<UpdateFrBatchRecord>

#### UpdateFrBatchRecord (class)

- id: string?
- title: string?
- body: string?
- description: string?
- priority: string?
- status: string?
- notes: string?
- acceptanceCriteria: IReadOnlyList<AcceptanceCriterion>?

#### CreateTrBatchRequest (class)

- records: IReadOnlyList<CreateTrBatchRecord>

#### CreateTrBatchRecord (class)

- id: string?
- title: string?
- body: string?
- description: string?
- priority: string?
- status: string?
- notes: string?
- acceptanceCriteria: IReadOnlyList<AcceptanceCriterion>?

#### UpdateTrBatchRequest (class)

- records: IReadOnlyList<UpdateTrBatchRecord>

#### UpdateTrBatchRecord (class)

- id: string?
- title: string?
- body: string?
- description: string?
- priority: string?
- status: string?
- notes: string?
- acceptanceCriteria: IReadOnlyList<AcceptanceCriterion>?

#### CreateTestBatchRequest (class)

- records: IReadOnlyList<CreateTestBatchRecord>

#### CreateTestBatchRecord (class)

- id: string?
- condition: string?
- description: string?
- title: string?
- priority: string?
- status: string?
- notes: string?
- acceptanceCriteria: IReadOnlyList<AcceptanceCriterion>?

#### UpdateTestBatchRequest (class)

- records: IReadOnlyList<UpdateTestBatchRecord>

#### UpdateTestBatchRecord (class)

- id: string?
- condition: string?
- description: string?
- title: string?
- priority: string?
- status: string?
- notes: string?
- acceptanceCriteria: IReadOnlyList<AcceptanceCriterion>?

#### CreateRequirementsBatchRequest (class)

- records: IReadOnlyList<CreateRequirementBatchRecord>

#### CreateRequirementBatchRecord (class)

- kind: string?
- id: string?
- title: string?
- body: string?
- condition: string?
- description: string?
- priority: string?
- status: string?
- notes: string?
- acceptanceCriteria: IReadOnlyList<AcceptanceCriterion>?

#### UpdateRequirementsBatchRequest (class)

- records: IReadOnlyList<UpdateRequirementBatchRecord>

#### UpdateRequirementBatchRecord (class)

- kind: string?
- id: string?
- title: string?
- body: string?
- condition: string?
- description: string?
- priority: string?
- status: string?
- notes: string?
- acceptanceCriteria: IReadOnlyList<AcceptanceCriterion>?

#### RequirementsBatchResult (class)

- success: bool
- operation: string
- kind: string?
- total: int
- items: IReadOnlyList<RequirementsBatchItem>
- errors: IReadOnlyList<RequirementsBatchError>

#### RequirementsBatchItem (class)

- kind: string
- id: string
- fr: FrEntry?
- tr: TrEntry?
- test: TestEntry?

#### RequirementsBatchError (class)

- index: int
- kind: string?
- id: string?
- error: string

#### UpsertFrTrMappingRequest (class)

- trIds: IReadOnlyList<string>
- testIds: IReadOnlyList<string>

#### CopyAcceptanceCriteriaFromTodoRequest (class)

- todoId: string

#### RequirementsMutationResult (class)

- success: bool
- error: string?

#### RequirementsGeneratedDocument (class)

- No [JsonPropertyName] properties declared in this client DTO.

#### RequirementsDocumentExportResult (class)

- success: bool
- format: string
- docType: string
- generatedAtUtc: DateTimeOffset
- outputRoot: string
- files: IReadOnlyList<RequirementsDocumentExportFile>

#### RequirementsDocumentExportFile (class)

- relativePath: string
- fullPath: string
- contentType: string
- lastModifiedUtc: DateTimeOffset

#### RequirementsIngestRequest (class)

- sourceFormat: string?
- preferredWikiFormat: string?
- documents: IReadOnlyDictionary<string, RequirementsIngestDocument>?
- functionalMarkdown: string?
- technicalMarkdown: string?
- testingMarkdown: string?
- mappingMarkdown: string?

#### RequirementsIngestDocument (class)

- content: string?
- contentBase64: string?
- lastModifiedUtc: DateTimeOffset?

#### RequirementsIngestResult (class)

- functionalParsed: int
- functionalAdded: int
- functionalUpdated: int
- functionalDeleted: int
- functionalIgnored: int
- technicalParsed: int
- technicalAdded: int
- technicalUpdated: int
- technicalDeleted: int
- technicalIgnored: int
- testingParsed: int
- testingAdded: int
- testingUpdated: int
- testingDeleted: int
- testingIgnored: int
- mappingParsed: int
- mappingAdded: int
- mappingUpdated: int
- mappingDeleted: int
- mappingIgnored: int
- selectedWikiFormat: string?
- selectedWikiReason: string?
- selectedManifestGeneratedAtUtc: DateTimeOffset?
- selectedLatestFileModifiedUtc: DateTimeOffset?
- warnings: IReadOnlyList<string>

### SessionLogModels.cs

#### UnifiedSessionLogDto (class)

- sourceType: string?
- sessionId: string?
- title: string?
- model: string?
- started: string?
- lastUpdated: string?
- status: string?
- turnCount: int
- workspace: WorkspaceInfoDto?
- turns: List<UnifiedRequestEntryDto>?
- totalTokens: int?
- cursorSessionLabel: string?
- copilotStatistics: CopilotStatisticsDto?

#### WorkspaceInfoDto (class)

- project: string?
- targetFramework: string?
- repository: string?
- branch: string?

#### UnifiedRequestEntryDto (class)

- requestId: string?
- timestamp: string?
- queryText: string?
- queryTitle: string?
- response: string?
- interpretation: string?
- status: string?
- actions: List<UnifiedActionDto>?
- model: string?
- tokenCount: int?
- tags: List<string>?
- contextList: List<string>?
- rawContext: object?
- modelProvider: string?
- failureNote: string?
- score: double?
- isPremium: bool?
- originalEntry: object?
- processingDialog: List<ProcessingDialogItemDto>?
- commits: List<SessionLogCommitDto>?
- designDecisions: List<string>?
- requirementsDiscovered: List<string>?
- filesModified: List<string>?
- blockers: List<string>?

#### CopilotStatisticsDto (class)

- averageSuccessScore: double?
- totalNetTokens: int?
- totalNetPremiumRequests: int?
- completedCount: int?
- inProgressCount: int?

#### UnifiedActionDto (class)

- order: int
- description: string?
- type: string?
- status: string?
- filePath: string?

#### ProcessingDialogItemDto (class)

- timestamp: string?
- role: string?
- content: string?
- category: string?

#### SessionLogCommitDto (class)

- sha: string?
- branch: string?
- message: string?
- author: string?
- timestamp: string?
- filesChanged: List<string>?

#### SessionLogQueryRequest (class)

- agent: string?
- agentDefinitionId: string?
- model: string?
- text: string?
- from: DateTimeOffset?
- to: DateTimeOffset?
- limit: int
- offset: int

#### SessionLogQueryResult (class)

- totalCount: int
- limit: int
- offset: int
- items: IReadOnlyList<UnifiedSessionLogDto>

#### SessionLogWorkspaceStampRepairResult (class)

- repaired: int
- dryRun: bool

#### SessionLogSubmitResult (class)

- id: long
- sourceType: string?
- sessionId: string?

#### SessionLogTurnSubmitResult (class)

- turnId: long
- agent: string?
- sessionId: string?
- requestId: string?

#### DialogAppendResult (class)

- agent: string?
- sessionId: string?
- requestId: string?
- totalDialogCount: int

#### SessionLifecycleOpenResult (class)

- agent: string?
- sessionId: string?
- created: bool

#### SessionLogMutationResult (class)

- turnId: long
- agent: string?
- sessionId: string?
- requestId: string?
- section: string?
- itemKey: string?
- replaced: bool
- cleared: bool
- deleted: bool

### TemplateModels.cs

#### TemplateItem (class)

- id: string
- title: string
- category: string
- tags: IReadOnlyList<string>
- description: string?
- engine: string
- variables: IReadOnlyList<TemplateVariableItem>
- content: string

#### TemplateVariableItem (class)

- name: string
- description: string?
- required: bool
- example: string?
- defaultValue: string?

#### TemplateCreateRequest (class)

- id: string
- title: string
- category: string
- content: string
- tags: IReadOnlyList<string>?
- description: string?
- engine: string?
- variables: IReadOnlyList<TemplateVariableItem>?

#### TemplateUpdateRequest (class)

- title: string?
- category: string?
- content: string?
- tags: IReadOnlyList<string>?
- description: string?
- engine: string?
- variables: IReadOnlyList<TemplateVariableItem>?

#### TemplateQueryResult (class)

- items: IReadOnlyList<TemplateItem>
- totalCount: int

#### TemplateMutationResult (class)

- success: bool
- error: string?
- item: TemplateItem?

#### TemplateTestRequest (class)

- variables: Dictionary<string, object?>?
- inlineTemplate: string?

#### TemplateTestResult (class)

- success: bool
- renderedContent: string?
- error: string?
- missingVariables: IReadOnlyList<string>?

#### TemplateResolveRequest (class)

- values: Dictionary<string, object?>?

#### TemplateResolveResult (class)

- success: bool
- templateId: string?
- prompt: string?
- error: string?
- missingVariables: IReadOnlyList<string>?

### TodoModels.cs

#### TodoFlatItem (class)

- id: string
- title: string
- section: string
- priority: string
- done: bool
- estimate: string?
- note: string?
- description: IReadOnlyList<string>?
- technicalDetails: IReadOnlyList<string>?
- implementationTasks: IReadOnlyList<TodoFlatTask>?
- completedDate: string?
- doneSummary: string?
- remaining: string?
- priorityNote: string?
- reference: string?
- phase: string?
- dependsOn: IReadOnlyList<string>?
- functionalRequirements: IReadOnlyList<string>?
- technicalRequirements: IReadOnlyList<string>?

#### TodoFlatTask (class)

- task: string
- done: bool

#### TodoCreateRequest (class)

- id: string
- title: string
- section: string
- priority: string
- estimate: string?
- description: IReadOnlyList<string>?
- technicalDetails: IReadOnlyList<string>?
- implementationTasks: IReadOnlyList<TodoFlatTask>?
- note: string?
- remaining: string?
- phase: string?
- dependsOn: IReadOnlyList<string>?
- functionalRequirements: IReadOnlyList<string>?
- technicalRequirements: IReadOnlyList<string>?

#### TodoUpdateRequest (class)

- title: string?
- priority: string?
- section: string?
- done: bool?
- estimate: string?
- description: IReadOnlyList<string>?
- technicalDetails: IReadOnlyList<string>?
- implementationTasks: IReadOnlyList<TodoFlatTask>?
- note: string?
- completedDate: string?
- doneSummary: string?
- remaining: string?
- reference: string?
- phase: string?
- dependsOn: IReadOnlyList<string>?
- functionalRequirements: IReadOnlyList<string>?
- technicalRequirements: IReadOnlyList<string>?

#### TodoMoveRequest (class)

- targetWorkspacePath: string

#### TodoQueryResult (class)

- items: IReadOnlyList<TodoFlatItem>
- totalCount: int

#### TodoMutationResult (class)

- success: bool
- error: string?
- item: TodoFlatItem?
- failureKind: TodoMutationFailureKind

#### TodoMutationFailureKind (enum)

- None
- Validation
- Conflict
- NotFound
- ProjectionFailed
- ExternalSyncFailed

#### TodoAuditQueryResult (class)

- entries: IReadOnlyList<TodoAuditEntry>
- totalCount: int

#### TodoProjectionStatusResult (class)

- authoritativeStore: string
- authoritativeDataSource: string
- projectionTargetPath: string
- projectionTargetExists: bool
- projectionConsistent: bool
- repairRequired: bool
- verifiedAtUtc: string
- lastImportedFromYamlUtc: string?
- lastProjectedToYamlUtc: string?
- lastProjectionFailureUtc: string?
- lastProjectionFailure: string?
- message: string?

#### TodoProjectionRepairResult (class)

- success: bool
- error: string?
- status: TodoProjectionStatusResult

#### TodoAuditEntry (class)

- auditId: long
- todoId: string
- version: int
- action: string
- recordedAtUtc: string
- snapshot: TodoFlatItem?
- previousSnapshot: TodoFlatItem?
- source: string?

#### RequirementsAnalysisResult (class)

- success: bool
- functionalRequirements: IReadOnlyList<string>?
- technicalRequirements: IReadOnlyList<string>?
- error: string?
- copilotResponse: string?

#### TodoExecutionStatus (enum)

- Draft
- Planned
- TestDesign
- TestReady
- Implementing
- Validating
- Blocked
- Complete
- Cancelled

#### TodoExecutionPriority (enum)

- Low
- Medium
- High
- Critical

#### TodoIterationPhaseStatus (enum)

- Planning
- Implementing
- Validating
- Complete
- Blocked
- Cancelled

#### TodoCheckpointKind (enum)

- PlanningDecision
- TestDefined
- TestPassing
- ImplementationProgress
- ValidationPassed
- ValidationFailed
- Blocker
- DeviceValidation
- CommitCreated
- RequirementRefined

#### AdbStepAction (enum)

- Screenshot
- Tap
- Swipe
- Text
- Keyevent
- Wait
- LaunchApp
- GetFocus

#### AcceptanceCriterion (class)

- id: string
- text: string
- isSatisfied: bool
- evidence: string?

#### TodoConstraint (class)

- id: string
- text: string
- source: string?

#### TodoDependency (class)

- todoId: string
- reason: string

#### TodoTestPlan (class)

- unitTestsDefined: bool
- unitTestsPassing: bool
- integrationTestsDefined: bool
- integrationTestsPassing: bool
- testFilePaths: IReadOnlyList<string>
- testCommands: IReadOnlyList<string>

#### TodoValidationState (class)

- lastResult: string
- lastValidatedAtUtc: string?
- validationArtifactIds: IReadOnlyList<string>
- summary: string?

#### TodoExecutionPointers (class)

- lastRelevantTurnId: string?
- lastSuccessfulTurnId: string?
- lastFailedTurnId: string?
- lastCheckpointId: string?
- lastCommitSha: string?
- lastScreenshotArtifactId: string?

#### TodoIterationPhase (class)

- phaseId: string
- workspacePath: string
- name: string
- summary: string
- status: TodoIterationPhaseStatus
- requirementIds: IReadOnlyList<string>
- todoIds: IReadOnlyList<string>
- entryCriteria: IReadOnlyList<string>
- exitCriteria: IReadOnlyList<string>
- createdFromPlanId: string?
- branch: string?
- createdAtUtc: string
- updatedAtUtc: string

#### TodoCheckpoint (class)

- checkpointId: string
- todoId: string
- workspacePath: string
- kind: TodoCheckpointKind
- summary: string
- nextAction: string?
- requirementIds: IReadOnlyList<string>
- sessionTurnIds: IReadOnlyList<string>
- artifactIds: IReadOnlyList<string>
- commitShas: IReadOnlyList<string>
- createdAtUtc: string

#### TodoExecutionRecord (class)

- todoId: string
- workspacePath: string
- title: string
- goal: string
- summary: string
- status: TodoExecutionStatus
- priority: TodoExecutionPriority
- iterationPhaseId: string?
- parentTodoId: string?
- childTodoIds: IReadOnlyList<string>
- dependsOn: IReadOnlyList<TodoDependency>
- blockedBy: IReadOnlyList<TodoDependency>
- acceptanceCriteria: IReadOnlyList<AcceptanceCriterion>
- constraints: IReadOnlyList<TodoConstraint>
- requirementIds: IReadOnlyList<string>
- relevantFiles: IReadOnlyList<string>
- artifactIds: IReadOnlyList<string>
- sessionTurnIds: IReadOnlyList<string>
- nextAction: string?
- testPlan: TodoTestPlan
- validation: TodoValidationState
- pointers: TodoExecutionPointers
- createdAtUtc: string
- updatedAtUtc: string

#### ActiveTodoContext (class)

- todoId: string
- workspacePath: string
- title: string
- goal: string
- summary: string
- status: TodoExecutionStatus
- iterationPhaseId: string?
- nextAction: string?
- requirementIds: IReadOnlyList<string>
- recentRequirementSnippets: IReadOnlyList<string>
- recentTurnSummaries: IReadOnlyList<string>
- relevantFiles: IReadOnlyList<string>
- artifactIds: IReadOnlyList<string>
- acceptanceCriteria: IReadOnlyList<string>
- constraints: IReadOnlyList<string>
- testPlan: TodoTestPlan
- validation: TodoValidationState
- pointers: TodoExecutionPointers

#### TodoDeltaContext (class)

- todoId: string
- sinceCheckpointId: string?
- newTurnIds: IReadOnlyList<string>
- newTurnSummaries: IReadOnlyList<string>
- newArtifactIds: IReadOnlyList<string>
- newCommitShas: IReadOnlyList<string>
- updatedNextAction: string?

#### CreateIterationPhaseRequest (class)

- name: string
- summary: string
- requirementIds: IReadOnlyList<string>?
- entryCriteria: IReadOnlyList<string>?
- exitCriteria: IReadOnlyList<string>?
- createdFromPlanId: string?
- branch: string?

#### CreateIterationPhaseResult (class)

- phaseId: string
- status: TodoIterationPhaseStatus

#### PlanTodoInput (class)

- title: string
- goal: string
- summary: string
- acceptanceCriteria: IReadOnlyList<string>?
- constraints: IReadOnlyList<string>?
- requirementIds: IReadOnlyList<string>?
- relevantFiles: IReadOnlyList<string>?
- dependsOnTodoIds: IReadOnlyList<string>?

#### CreateTodosFromPlanRequest (class)

- phaseId: string
- planId: string
- todos: IReadOnlyList<PlanTodoInput>?

#### CreateTodosFromPlanResult (class)

- phaseId: string
- todoIds: IReadOnlyList<string>

#### ActiveTodoResult (class)

- todoId: string
- title: string
- status: TodoExecutionStatus
- nextAction: string?

#### SetTodoTestPlanRequest (class)

- unitTestsDefined: bool
- unitTestsPassing: bool?
- integrationTestsDefined: bool
- integrationTestsPassing: bool?
- testFilePaths: IReadOnlyList<string>?
- testCommands: IReadOnlyList<string>?

#### SetTodoTestPlanResult (class)

- todoId: string
- status: TodoExecutionStatus

#### UpdateTodoStatusRequest (class)

- targetStatus: TodoExecutionStatus
- reason: string?

#### UpdateTodoStatusResult (class)

- todoId: string
- previousStatus: TodoExecutionStatus
- currentStatus: TodoExecutionStatus

#### AppendTodoCheckpointRequest (class)

- kind: TodoCheckpointKind
- summary: string
- nextAction: string?
- requirementIds: IReadOnlyList<string>?
- sessionTurnIds: IReadOnlyList<string>?
- artifactIds: IReadOnlyList<string>?
- commitShas: IReadOnlyList<string>?

#### AppendTodoCheckpointResult (class)

- checkpointId: string
- todoId: string

#### RecordTodoValidationResultRequest (class)

- result: string
- summary: string?
- artifactIds: IReadOnlyList<string>?
- sessionTurnIds: IReadOnlyList<string>?
- unitTestsPassing: bool?
- integrationTestsPassing: bool?

#### RecordTodoValidationResultResult (class)

- todoId: string
- validationState: TodoValidationState

#### LinkTodoToSessionTurnsRequest (class)

- sessionTurnIds: IReadOnlyList<string>?

#### LinkTodoToSessionTurnsResult (class)

- todoId: string
- sessionTurnIds: IReadOnlyList<string>

#### AdbStepRequest (class)

- deviceSerial: string?
- action: AdbStepAction
- captureScreenshot: bool
- instruction: string?
- x: int?
- y: int?
- startX: int?
- startY: int?
- endX: int?
- endY: int?
- durationMs: int?
- text: string?
- keyEvent: string?
- packageName: string?
- activityName: string?
- waitMilliseconds: int?

#### AdbStepResult (class)

- success: bool
- action: AdbStepAction
- deviceSerial: string?
- commandSummary: string?
- screenshotPath: string?
- screenshotBase64: string?
- currentFocus: string?
- observationHints: IReadOnlyList<string>
- error: string?
- timestampUtc: string

### ToolRegistryModels.cs

#### ToolDto (class)

- id: int
- name: string
- description: string
- tags: IReadOnlyList<string>
- parameterSchema: string?
- commandTemplate: string?
- workspacePath: string?
- dateTimeCreated: DateTimeOffset
- dateTimeModified: DateTimeOffset

#### ToolCreateRequest (class)

- name: string
- description: string
- tags: IReadOnlyList<string>
- parameterSchema: string?
- commandTemplate: string?
- workspacePath: string?

#### ToolUpdateRequest (class)

- name: string?
- description: string?
- tags: IReadOnlyList<string>?
- parameterSchema: string?
- commandTemplate: string?
- workspacePath: string?

#### ToolSearchResult (class)

- tools: IReadOnlyList<ToolDto>
- totalCount: int

#### ToolMutationResult (class)

- success: bool
- error: string?
- tool: ToolDto?

#### BucketDto (class)

- id: int
- name: string
- owner: string
- repo: string
- branch: string
- manifestPath: string
- dateTimeCreated: DateTimeOffset
- dateTimeLastSynced: DateTimeOffset?

#### BucketAddRequest (class)

- name: string
- owner: string
- repo: string
- branch: string?
- manifestPath: string?

#### BucketListResult (class)

- buckets: IReadOnlyList<BucketDto>
- totalCount: int

#### BucketMutationResult (class)

- success: bool
- error: string?
- bucket: BucketDto?

#### ToolManifest (class)

- name: string
- description: string
- tags: IReadOnlyList<string>
- parameterSchema: string?
- commandTemplate: string?
- manifestFile: string

#### BucketBrowseResult (class)

- success: bool
- error: string?
- tools: IReadOnlyList<ToolManifest>?

#### BucketSyncResult (class)

- success: bool
- error: string?
- updated: int
- added: int
- unchanged: int

### TransactionSecurityModels.cs

#### PartyRegistrationRequest (class)

- partyId: string
- role: string
- activeSigningKeyId: string?
- activeEncryptionKeyId: string?
- signingPublicKeyPem: string?
- signingPrivateKeyPem: string?
- encryptionPublicKeyPem: string?
- status: string

#### PartyRegistrationResponse (class)

- partyId: string
- role: string
- activeSigningKeyId: string?
- activeEncryptionKeyId: string?
- status: string
- createdAtUtc: DateTimeOffset
- updatedAtUtc: DateTimeOffset?

#### PartyKeyDescriptor (class)

- partyId: string
- keyId: string
- purpose: string
- algorithm: string
- publicKeyPem: string
- status: string
- createdAtUtc: DateTimeOffset
- expiresAtUtc: DateTimeOffset?

#### TransactionManifestSignRequest (class)

- transactionId: string
- turnId: string?
- publisherPartyId: string
- subscriberPartyId: string
- publisherSigningKeyId: string?
- subscriberEncryptionKeyId: string?
- sequence: long
- nonce: string
- issuedAtUtc: DateTimeOffset?
- expiresAtUtc: DateTimeOffset?
- diffgramSha256: string
- encryptedBodySha256: string
- algorithms: TransactionManifestAlgorithms

#### TransactionManifestAlgorithms (class)

- signature: string
- encryption: string
- canonicalization: string

#### TransactionManifestSignResponse (class)

- success: bool
- manifest: TransactionManifestDto?
- reason: TransactionFailureReason

#### TransactionManifestVerifyRequest (class)

- manifest: TransactionManifestDto
- expectedSubscriberPartyId: string?

#### TransactionManifestVerifyResponse (class)

- isValid: bool
- reason: TransactionFailureReason
- manifestHashSha256: string?

#### TransactionManifestTraceRecord (class)

- transactionId: string
- turnId: string?
- publisherPartyId: string
- publisherSigningKeyId: string?
- subscriberPartyId: string
- subscriberEncryptionKeyId: string?
- sequence: long
- nonce: string
- issuedAtUtc: DateTimeOffset
- expiresAtUtc: DateTimeOffset
- diffgramSha256: string
- encryptedBodySha256: string
- signatureAlgorithm: string
- encryptionAlgorithm: string
- canonicalizationProfile: string
- signatureKeyId: string
- signatureValue: string
- signedAtUtc: DateTimeOffset
- manifestHashSha256: string
- status: string
- createdAtUtc: DateTimeOffset

#### TransactionManifestTraceReportRequest (class)

- publisherPartyId: string?
- subscriberPartyId: string?
- status: string?
- fromUtc: DateTimeOffset?
- toUtc: DateTimeOffset?
- limit: int?

#### TransactionManifestTraceReport (class)

- generatedAtUtc: DateTimeOffset
- publisherPartyId: string?
- subscriberPartyId: string?
- status: string?
- fromUtc: DateTimeOffset?
- toUtc: DateTimeOffset?
- limit: int
- totalCount: int
- returnedCount: int
- records: List<TransactionManifestTraceRecord>

#### TransactionManifestDto (class)

- transactionId: string
- turnId: string?
- publisherPartyId: string
- subscriberPartyId: string
- publisherSigningKeyId: string?
- subscriberEncryptionKeyId: string?
- sequence: long
- nonce: string
- issuedAtUtc: DateTimeOffset
- expiresAtUtc: DateTimeOffset
- diffgramSha256: string
- encryptedBodySha256: string
- algorithms: TransactionManifestAlgorithms
- signature: TransactionManifestSignatureDto?

#### TransactionManifestSignatureDto (class)

- algorithm: string
- keyId: string
- value: string
- signedAtUtc: DateTimeOffset

#### DiffgramCommitRequest (class)

- manifest: TransactionManifestDto
- encryptedDiffgramBase64: string
- encryptedBodySha256: string
- diffgramSha256: string

#### DiffgramCommitResponse (class)

- status: string
- reason: TransactionFailureReason
- transactionId: string
- diffgramId: string?
- committedAtUtc: DateTimeOffset?

#### TransactionStatusResponse (class)

- transactionId: string
- status: string
- reason: TransactionFailureReason?
- committedAtUtc: DateTimeOffset?
- abortedAtUtc: DateTimeOffset?

#### TransactionAbortRequest (class)

- reason: TransactionFailureReason
- actor: string?

#### TransactionAbortResponse (class)

- transactionId: string
- status: string
- reason: TransactionFailureReason
- abortedAtUtc: DateTimeOffset

#### TurnTransactionStatusResponse (class)

- enabled: bool
- degraded: bool
- lastReason: TransactionFailureReason
- lastTransactionId: string?
- message: string

#### TransactionPubSubMessageStatus (class)

- operationId: string
- transactionId: string
- kind: string
- topicName: string
- subscriberId: string
- status: string
- attemptCount: int
- reason: TransactionFailureReason
- createdAtUtc: DateTimeOffset
- updatedAtUtc: DateTimeOffset

#### TransactionPubSubReplayResult (class)

- attemptedCount: int
- acknowledgedCount: int
- pendingCount: int

#### TransactionPubSubRetentionResult (class)

- completedBeforeUtc: DateTimeOffset
- maxMessages: int
- purgedCount: int
- retainedPendingCount: int

#### TransactionFailureReason (enum)

- None
- Unknown
- UnknownParty
- DisabledParty
- UnknownKey
- DisabledKey
- ExpiredManifest
- FutureManifest
- ReplayNonce
- StaleSequence
- MalformedSignature
- ManifestSignatureMismatch
- EncryptedBodyHashMismatch
- PlaintextDiffgramHashMismatch
- WrongSubscriber
- DecryptFailed
- DuplicateConflict
- Aborted
- KeyServerUnavailable
- SubscriberUnavailable
- CommitTimeout
- TransactionsDisabled
- DeferredFeatureDisabled

### TriageModels.cs

#### TriageReportRequest (record)

- title: required string
- summary: required string
- observedBehavior: string?
- expectedBehavior: string?
- severity: string?
- component: string?
- dedupeKey: string?
- errorSignature: string?
- affectedPaths: IReadOnlyList<string>?
- affectedSymbols: IReadOnlyList<string>?
- evidence: IReadOnlyDictionary<string, string>?
- reproductionHints: IReadOnlyList<string>?
- tags: IReadOnlyList<string>?
- reporterAgent: string?
- sessionId: string?
- turnId: string?
- currentTodoId: string?
- workspacePath: string?
- idempotencyKey: string?

#### TriageReportSubmitResult (record)

- success: required bool
- error: string?
- reportId: string
- groupId: string
- status: string
- quietDeadlineUtc: DateTimeOffset
- workspacePath: string

#### TriageReportDetail (record)

- reportId: required string
- groupId: required string
- status: required string
- title: string?
- summary: string?
- originalWorkspacePath: string?
- workspacePath: string?
- createdUtc: DateTimeOffset

#### TriageGroupDetail (record)

- groupId: required string
- status: required string
- reportCount: required int
- workspacePath: string?
- title: string?
- summary: string?
- quietDeadlineUtc: DateTimeOffset
- createdTodoId: string?
- lastError: string?
- reports: IReadOnlyList<TriageReportDetail>

#### TriageGroupQueryResult (record)

- items: IReadOnlyList<TriageGroupDetail>
- totalCount: int

#### TriageResearchRunDetail (record)

- runId: required string
- groupId: required string
- status: required string
- workspacePath: string?
- groupStatus: string?
- groupTitle: string?
- groupSummary: string?
- reportCount: int
- promptTemplateId: string?
- prompt: string?
- groupJson: string?
- rawOutput: string?
- responseJson: string?
- error: string?
- createdTodoId: string?
- startedUtc: DateTimeOffset
- completedUtc: DateTimeOffset?

#### TriageRunQueryResult (record)

- items: IReadOnlyList<TriageResearchRunDetail>
- totalCount: int

#### TriageCreatedTodoDetail (record)

- todoId: required string
- createdAtUtc: DateTimeOffset
- workspacePath: string?
- groupId: string?
- runId: string?
- groupStatus: string?
- runStatus: string?
- groupTitle: string?
- groupSummary: string?
- reportCount: int
- quietDeadlineUtc: DateTimeOffset?

#### TriageCreatedTodoQueryResult (record)

- items: IReadOnlyList<TriageCreatedTodoDetail>
- totalCount: int

#### TriageDashboardResult (record)

- triageQueue: IReadOnlyList<TriageGroupDetail>
- reportGroupQueue: IReadOnlyList<TriageGroupDetail>
- runHistory: IReadOnlyList<TriageResearchRunDetail>
- totalGroupCount: int
- totalRunCount: int

#### TriageGroupSelectionRequest (record)

- groupIds: IReadOnlyList<string>?
- reportIds: IReadOnlyList<string>?
- title: string?
- summary: string?

#### TriageGroupEditResult (record)

- group: TriageGroupDetail
- removedGroupIds: IReadOnlyList<string>
- movedReportCount: int

### TunnelModels.cs

#### TunnelProviderInfo (class)

- provider: string
- enabled: bool
- isRunning: bool
- publicUrl: string?
- error: string?

### VoiceModels.cs

#### VoiceSessionCreateRequest (class)

- language: string?
- deviceId: string?
- clientName: string?
- workspacePath: string?
- agentName: string?
- agentPath: string?
- agentModel: string?
- agentSeed: string?
- agentPrompt: string?
- agentParameters: Dictionary<string, string>?
- executionStrategy: string?
- oneShotSession: bool

#### VoiceSessionCreateResponse (class)

- sessionId: string
- status: string
- language: string
- modelRequested: string?
- modelResolved: string?
- executionStrategy: string

#### VoiceTurnRequest (class)

- userTranscriptText: string
- language: string?
- clientTimestampUtc: string?

#### VoiceToolCallRecord (class)

- turnId: string
- toolName: string
- step: int
- argumentsJson: string
- status: string
- isMutation: bool
- resultSummary: string?
- error: string?

#### VoiceTurnResponse (class)

- sessionId: string
- turnId: string
- status: string
- assistantDisplayText: string?
- assistantSpeakText: string?
- toolCalls: IReadOnlyList<VoiceToolCallRecord>?
- error: string?
- latencyMs: int
- modelRequested: string?
- modelResolved: string?

#### VoiceInterruptResponse (class)

- sessionId: string
- interrupted: bool
- status: string

#### VoiceSessionStatus (class)

- sessionId: string
- status: string
- language: string
- createdUtc: string
- lastUpdatedUtc: string
- isTurnActive: bool
- executionStrategy: string
- lastError: string?
- lastTurnId: string?
- turnCounter: int
- transcriptCount: int

#### VoiceTranscriptEntry (class)

- timestampUtc: string
- turnId: string?
- role: string
- category: string
- text: string

#### VoiceTranscriptResponse (class)

- sessionId: string
- items: IReadOnlyList<VoiceTranscriptEntry>

#### VoiceEscapeResponse (class)

- sent: bool

#### VoiceTurnStreamEvent (class)

- type: string
- text: string?
- turnId: string?
- status: string?
- message: string?
- toolName: string?
- summary: string?
- toolCalls: IReadOnlyList<VoiceToolCallRecord>?
- latencyMs: int?

### WorkspaceModels.cs

#### WorkspaceDto (class)

- workspacePath: string
- name: string
- todoPath: string
- dataDirectory: string?
- tunnelProvider: string?
- isPrimary: bool
- isEnabled: bool
- dateTimeCreated: DateTimeOffset
- dateTimeModified: DateTimeOffset
- runAs: string?
- promptTemplate: string?
- statusPrompt: string
- implementPrompt: string
- planPrompt: string
- bannedLicenses: List<string>
- bannedCountriesOfOrigin: List<string>
- bannedOrganizations: List<string>
- bannedIndividuals: List<string>
- gitRemoteUrl: string?

#### WorkspaceCreateRequest (class)

- workspacePath: string
- name: string?
- todoPath: string?
- dataDirectory: string?
- tunnelProvider: string?
- runAs: string?
- isPrimary: bool
- isEnabled: bool
- promptTemplate: string?
- statusPrompt: string?
- implementPrompt: string?
- planPrompt: string?
- bannedLicenses: List<string>?
- bannedCountriesOfOrigin: List<string>?
- bannedOrganizations: List<string>?
- bannedIndividuals: List<string>?

#### WorkspaceUpdateRequest (class)

- name: string?
- todoPath: string?
- dataDirectory: string?
- tunnelProvider: string?
- runAs: string?
- isPrimary: bool?
- isEnabled: bool?
- promptTemplate: string?
- statusPrompt: string?
- implementPrompt: string?
- planPrompt: string?
- bannedLicenses: List<string>?
- bannedCountriesOfOrigin: List<string>?
- bannedOrganizations: List<string>?
- bannedIndividuals: List<string>?

#### WorkspacePolicyApplyRequest (class)

- directive: string
- workspacePath: string?

#### WorkspacePolicyDirective (class)

- action: string
- category: string
- values: IReadOnlyList<string>
- scope: string
- scopeWorkspacePath: string?
- parser: string?

#### WorkspacePolicyMutationResult (class)

- workspacePath: string
- workspaceName: string
- success: bool
- error: string?
- beforeValues: IReadOnlyList<string>
- afterValues: IReadOnlyList<string>

#### WorkspacePolicyApplyResult (class)

- success: bool
- error: string?
- parsedDirective: WorkspacePolicyDirective?
- workspaceResults: IReadOnlyList<WorkspacePolicyMutationResult>

#### GlobalPromptResult (class)

- template: string
- isDefault: bool

#### GlobalPromptUpdateRequest (class)

- template: string?

#### MarkerRegenerationResult (class)

- regenerated: bool
- workspaceCount: int

#### WorkspaceListResult (class)

- items: IReadOnlyList<WorkspaceDto>
- totalCount: int

#### WorkspaceMutationResult (class)

- success: bool
- error: string?
- workspace: WorkspaceDto?

#### WorkspaceInitResult (class)

- success: bool
- error: string?
- filesCreated: IReadOnlyList<string>?

#### WorkspaceProcessStatus (class)

- isRunning: bool
- pid: int?
- uptime: string?
- port: int?
- error: string?


