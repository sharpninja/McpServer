# TEST-MCP-HOSTILEREVIEW

[Back to Testing Requirements](Testing-Requirements)

## TEST-MCP-HOSTILEREVIEW

### TEST-MCP-HOSTILEREVIEW-001

HostileReviewEntity_RoundTrip_SqlitePgSqlServer; HostileReviewSubmit_ValidRequest_CreatesQueueItem; HostileReviewSubmit_OversizedPayload_Rejected; HostileReviewSubmit_ForeignWorkspace_403.

**Acceptance Criteria:**
- [x] Named tests exist: HostileReviewEntity_RoundTrip_SqlitePgSqlServer; HostileReviewSubmit_ValidRequest_CreatesQueueItem; HostileReviewSubmit_OversizedPayload_Rejected; HostileReviewSubmit_ForeignWorkspace_403. (evidence: tests/McpServer.Support.Mcp.Tests/Services/HostileReviewG4OverlayTests.cs; overlay G4 SHA-256 BFD7029944F1EAAC99F2808EA47146DBBD7C87EEB3E02AB4ADC31F95D3F0DFD1)

### TEST-MCP-HOSTILEREVIEW-002

HostileReviewSubmit_MissingArtifact_ReturnsDiagnosticNotSilentOmit; HostileReviewSubmit_StaleOrUnauthorizedLink_Diagnostic; HostileReviewSubmit_AmbiguousLink_Diagnostic; HostileReviewDispatch_ReauthorizesEveryArtifact; HostileReviewDispatch_ChangedHashOrRevokedAccess_FailsClosed; HostileReviewProtectedContext_NoRawPromptInQueueNotificationLogOrResult; HostileReviewResolvedInput_PerArtifactAndAggregateLimitsFailClosed; HostileReviewArtifact_SecretSentinelsNeverEcho; HostileReviewCrossWorkspaceArtifact_NeverResolves.

**Acceptance Criteria:**
- [x] Named tests exist: HostileReviewSubmit_MissingArtifact_ReturnsDiagnosticNotSilentOmit; HostileReviewSubmit_StaleOrUnauthorizedLink_Diagnostic; HostileReviewSubmit_AmbiguousLink_Diagnostic. (evidence: tests/McpServer.Support.Mcp.Tests/Services/HostileReviewG4OverlayTests.cs; overlay G4 SHA-256 BFD7029944F1EAAC99F2808EA47146DBBD7C87EEB3E02AB4ADC31F95D3F0DFD1)
- [x] Named tests exist: HostileReviewSubmit_MissingArtifact_ReturnsDiagnosticNotSilentOmit; HostileReviewSubmit_StaleOrUnauthorizedLink_Diagnostic; HostileReviewSubmit_AmbiguousLink_Diagnostic; HostileReviewDispatch_ReauthorizesEveryArtifact; HostileReviewDispatch_ChangedHashOrRevokedAccess_FailsClosed; HostileReviewProtectedContext_NoRawPromptInQueueNotificationLogOrResult; HostileReviewResolvedInput_PerArtifactAndAggregateLimitsFailClosed; HostileReviewArtifact_SecretSentinelsNeverEcho; HostileReviewCrossWorkspaceArtifact_NeverResolves. (evidence: tests/McpServer.Support.Mcp.Tests/Services/HostileReviewG4OverlayTests.cs; overlay G4 SHA-256 BFD7029944F1EAAC99F2808EA47146DBBD7C87EEB3E02AB4ADC31F95D3F0DFD1)

### TEST-MCP-HOSTILEREVIEW-003

HostileReviewExecution_RecordsModelEffortAgentTemplateRunId; HostileReviewExecution_OmitsFabricatedTokenCounts.

**Acceptance Criteria:**
- [x] Named tests exist: HostileReviewExecution_RecordsModelEffortAgentTemplateRunId; HostileReviewExecution_OmitsFabricatedTokenCounts. (evidence: tests/McpServer.Support.Mcp.Tests/Services/HostileReviewG4OverlayTests.cs; overlay G4 SHA-256 BFD7029944F1EAAC99F2808EA47146DBBD7C87EEB3E02AB4ADC31F95D3F0DFD1)
- [x] Add worker lifecycle tests that distinguish restart recovery from live-pool retry, permit more than one model execution only at declared process-loss boundaries, require one accepted terminal result across restart, lease loss, stale completion, retry exhaustion, internal cancellation, and model unavailability, and verify process-lifetime correlation and pool-job cleanup. (evidence: tests/McpServer.Support.Mcp.Tests/Services/HostileReviewG4OverlayTests.cs; overlay G4 SHA-256 BFD7029944F1EAAC99F2808EA47146DBBD7C87EEB3E02AB4ADC31F95D3F0DFD1)

### TEST-MCP-HOSTILEREVIEW-004

HostileReviewGet_NormalizedFindings_TaxonomyComplete; HostileReview_RequestQuality_ScoresDisclosureScopeObjectiveConfidenceContext.

**Acceptance Criteria:**
- [x] Named tests exist: HostileReviewGet_NormalizedFindings_TaxonomyComplete; HostileReview_RequestQuality_ScoresDisclosureScopeObjectiveConfidenceContext. (evidence: tests/McpServer.Support.Mcp.Tests/Services/HostileReviewG4OverlayTests.cs; overlay G4 SHA-256 BFD7029944F1EAAC99F2808EA47146DBBD7C87EEB3E02AB4ADC31F95D3F0DFD1)

### TEST-MCP-HOSTILEREVIEW-005

HostileReviewQuery_ByModelAndEffort_ReturnsOnlyMatchingRuns; HostileReviewQuery_ByRequesterAndTargetType_AndFilters; HostileReviewQuery_NoMatch_EmptyList.

**Acceptance Criteria:**
- [x] Named tests exist: HostileReviewQuery_ByModelAndEffort_ReturnsOnlyMatchingRuns; HostileReviewQuery_ByRequesterAndTargetType_AndFilters; HostileReviewQuery_NoMatch_EmptyList. (evidence: tests/McpServer.Support.Mcp.Tests/Services/HostileReviewG4OverlayTests.cs; overlay G4 SHA-256 BFD7029944F1EAAC99F2808EA47146DBBD7C87EEB3E02AB4ADC31F95D3F0DFD1)

### TEST-MCP-HOSTILEREVIEW-006

HostileReview_Default_DoesNotMutateProductFiles; HostileReview_SurfaceParity_RestReplDirectorPlugin.

**Acceptance Criteria:**
- [x] Named tests exist: HostileReview_Default_DoesNotMutateProductFiles; HostileReview_SurfaceParity_RestReplDirectorPlugin. (evidence: tests/McpServer.Support.Mcp.Tests/Services/HostileReviewG4OverlayTests.cs; overlay G4 SHA-256 BFD7029944F1EAAC99F2808EA47146DBBD7C87EEB3E02AB4ADC31F95D3F0DFD1)
- [x] Every status surface accepts only a request identity and performs no mutation. Mutation-shaped status input is rejected. The registered inventory remains exactly submit/status/get/query. Internal lifecycle cancellation creates no additional public operation. (evidence: tests/McpServer.Support.Mcp.Tests/Services/HostileReviewG4OverlayTests.cs; overlay G4 SHA-256 BFD7029944F1EAAC99F2808EA47146DBBD7C87EEB3E02AB4ADC31F95D3F0DFD1)

<!-- mcp-wiki-footer:begin -->
This wiki was generated by QBrain. [Explore the QBrain project on GitHub](https://github.com/sharpninja/McpServer).
<!-- mcp-wiki-footer:end -->
