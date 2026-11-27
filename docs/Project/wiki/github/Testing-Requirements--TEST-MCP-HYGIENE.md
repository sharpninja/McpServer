# TEST-MCP-HYGIENE

[Back to Testing Requirements](Testing-Requirements)

## TEST-MCP-HYGIENE

### TEST-MCP-HYGIENE-001

WorkspaceValidation_ResultContract_HasRequiredFields; WorkspaceValidation_CleanWorkspace_ZeroFindings; WorkspaceValidation_UnknownRuleCode_Diagnostic.

**Acceptance Criteria:**
- [x] Named tests exist: WorkspaceValidation_ResultContract_HasRequiredFields; WorkspaceValidation_CleanWorkspace_ZeroFindings; WorkspaceValidation_UnknownRuleCode_Diagnostic. (evidence: tests/McpServer.Support.Mcp.Tests/Services/WorkspaceHygieneG5OverlayTests.cs; G5 TRX SHA-256 7B3EA931BEDAE3D8D69A260BA9EAB516EDF8337BCF742712E4B4036DC30B7D16)

### TEST-MCP-HYGIENE-002

Rule_FrTrTest_MissingAcceptanceCriteria_FindsRecord; Rule_TrWithNoFr_Orphan; Rule_FrMissingTrOrTest_Orphan; Rule_TestWithNoFr_Orphan; Rule_BrokenOrDuplicateMapping.

**Acceptance Criteria:**
- [x] Named tests exist: Rule_FrTrTest_MissingAcceptanceCriteria_FindsRecord; Rule_TrWithNoFr_Orphan; Rule_FrMissingTrOrTest_Orphan; Rule_TestWithNoFr_Orphan; Rule_BrokenOrDuplicateMapping. (evidence: tests/McpServer.Support.Mcp.Tests/Services/WorkspaceHygieneG5OverlayTests.cs; G5 TRX SHA-256 7B3EA931BEDAE3D8D69A260BA9EAB516EDF8337BCF742712E4B4036DC30B7D16)

### TEST-MCP-HYGIENE-003

Rule_DoneTrue_IncompleteTasks; Rule_DoneFalse_AllTasksComplete; Rule_DoneWithoutDoneSummary; Rule_RemainingContradictsCompletion; Rule_MissingDependencyTarget; Rule_MissingReferencedRequirementId.

**Acceptance Criteria:**
- [x] Named tests exist: Rule_DoneTrue_IncompleteTasks; Rule_DoneFalse_AllTasksComplete; Rule_DoneWithoutDoneSummary; Rule_RemainingContradictsCompletion; Rule_MissingDependencyTarget; Rule_MissingReferencedRequirementId. (evidence: tests/McpServer.Support.Mcp.Tests/Services/WorkspaceHygieneG5OverlayTests.cs; G5 TRX SHA-256 7B3EA931BEDAE3D8D69A260BA9EAB516EDF8337BCF742712E4B4036DC30B7D16)

### TEST-MCP-HYGIENE-004

Rule_InProgressTurn_OlderThan48h_UtcClock; Rule_TriageNonTerminal_UsesLiveDomainEnum; Rule_StaleThresholdOverride_BoundedAuthenticatedRecorded; Validation_AuthRequired; Validation_CancellationHonored; Validation_LargeWorkspace_Paginates.

**Acceptance Criteria:**
- [x] Named tests exist: Rule_InProgressTurn_OlderThan48h_UtcClock; Rule_TriageNonTerminal_UsesLiveDomainEnum; Rule_StaleThresholdOverride_BoundedAuthenticatedRecorded; Validation_AuthRequired; Validation_CancellationHonored; Validation_LargeWorkspace_Paginates. (evidence: tests/McpServer.Support.Mcp.Tests/Services/WorkspaceHygieneG5OverlayTests.cs; G5 TRX SHA-256 7B3EA931BEDAE3D8D69A260BA9EAB516EDF8337BCF742712E4B4036DC30B7D16)

### TEST-MCP-HYGIENE-005

Parity_RestDirectorReplPlugin_SameRuleCodesAndCounts; Director_Exit1_WhenErrorSeverityPresent; Director_Exit0_WhenWarningOnly; Validation_NeverAutoRepairs.

**Acceptance Criteria:**
- [x] Named tests exist: Parity_RestDirectorReplPlugin_SameRuleCodesAndCounts; Director_Exit1_WhenErrorSeverityPresent; Director_Exit0_WhenWarningOnly; Validation_NeverAutoRepairs. (evidence: tests/McpServer.Support.Mcp.Tests/Services/WorkspaceHygieneG5OverlayTests.cs; G5 TRX SHA-256 7B3EA931BEDAE3D8D69A260BA9EAB516EDF8337BCF742712E4B4036DC30B7D16)
