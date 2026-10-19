# Custom views of the EF Core entity model (PLAN-C4MODEL-001 D5, D6), one DbContext area per group, chunked to the
# AC-5 letter-page standard. Migrated once by ~/.cache/c4-migration/Convert-EfErToDsl.ps1 (D9); edit here from now on.
custom "EF-CORE-01" "McpDbContext entities: workspaces, products, audit and context (1 of 2)" "EF Core entity model, McpDbContext, area Core. Columns are element properties." {
    include entWorkspace entWorkspaceBannedItem entProduct entProductWorkspaceMembership
    autolayout lr 100 100
}
custom "EF-CORE-02" "McpDbContext entities: workspaces, products, audit and context (2 of 2)" "EF Core entity model, McpDbContext, area Core. Columns are element properties." {
    include entDataAuditLog entContextDocument entContextChunk entWorkspace
    autolayout lr 100 100
}
custom "EF-SESSIONLOG-01" "McpDbContext entities: session logs (1 of 5)" "EF Core entity model, McpDbContext, area SessionLog. Columns are element properties." {
    include entSessionLog entSessionLogTag entSessionLogTurn entWorkspace entAgentDefinition
    autolayout tb 100 100
}
custom "EF-SESSIONLOG-02" "McpDbContext entities: session logs (2 of 5)" "EF Core entity model, McpDbContext, area SessionLog. Columns are element properties." {
    include entSessionLogAction entSessionLogCommit entWorkspace entSessionLogTurn
    autolayout lr 100 100
}
custom "EF-SESSIONLOG-03" "McpDbContext entities: session logs (3 of 5)" "EF Core entity model, McpDbContext, area SessionLog. Columns are element properties." {
    include entSessionLogProcessingDialog entSessionLogTurnContext entWorkspace entSessionLogTurn
    autolayout lr 100 100
}
custom "EF-SESSIONLOG-04" "McpDbContext entities: session logs (4 of 5)" "EF Core entity model, McpDbContext, area SessionLog. Columns are element properties." {
    include entSessionLogTurnStringList entSessionLogTurnTag entWorkspace entSessionLogTurn
    autolayout lr 100 100
}
custom "EF-SESSIONLOG-05" "McpDbContext entities: session logs (5 of 5)" "EF Core entity model, McpDbContext, area SessionLog. Columns are element properties." {
    include entSessionLogCommitFile entWorkspace entSessionLogCommit
    autolayout lr 100 100
}
custom "EF-AGENTS-01" "McpDbContext entities: agents, brain slots and tools (1 of 4)" "EF Core entity model, McpDbContext, area AgentsTools. Columns are element properties." {
    include entToolDefinition entToolDefinitionTag entToolBucket entWorkspace
    autolayout lr 100 100
}
custom "EF-AGENTS-02" "McpDbContext entities: agents, brain slots and tools (2 of 4)" "EF Core entity model, McpDbContext, area AgentsTools. Columns are element properties." {
    include entAgentDefinition entAgentDefinitionModel entAgentEventLog entWorkspace
    autolayout lr 100 100
}
custom "EF-AGENTS-03" "McpDbContext entities: agents, brain slots and tools (3 of 4)" "EF Core entity model, McpDbContext, area AgentsTools. Columns are element properties." {
    include entAgentWorkspace entAgentWorkspaceListItem entBrainSlotDefinition entWorkspace entAgentDefinition
    autolayout tb 100 100
}
custom "EF-AGENTS-04" "McpDbContext entities: agents, brain slots and tools (4 of 4)" "EF Core entity model, McpDbContext, area AgentsTools. Columns are element properties." {
    include entBrainSlotInvocation entWorkspace entBrainSlotDefinition
    autolayout lr 100 100
}
custom "EF-TODOREQ-01" "McpDbContext entities: TODOs, requirements and use cases (1 of 8)" "EF Core entity model, McpDbContext, area TodoRequirements. Columns are element properties." {
    include entTodoItem entTodoItemListItem entTodoItemTask entWorkspace
    autolayout lr 100 100
}
custom "EF-TODOREQ-02" "McpDbContext entities: TODOs, requirements and use cases (2 of 8)" "EF Core entity model, McpDbContext, area TodoRequirements. Columns are element properties." {
    include entTodoRequirementLink entWorkspace entTodoItem entRequirement
    autolayout lr 100 100
}
custom "EF-TODOREQ-03" "McpDbContext entities: TODOs, requirements and use cases (3 of 8)" "EF Core entity model, McpDbContext, area TodoRequirements. Columns are element properties." {
    include entTodoAuditHistory entTodoDocumentMetadata entTodoCompletedGroup entTodoDocumentNote entWorkspace
    autolayout lr 100 100
}
custom "EF-TODOREQ-04" "McpDbContext entities: TODOs, requirements and use cases (4 of 8)" "EF Core entity model, McpDbContext, area TodoRequirements. Columns are element properties." {
    include entTodoCompletedItem entRequirement entRequirementAcceptanceCriterion entWorkspace entTodoCompletedGroup
    autolayout lr 100 100
}
custom "EF-TODOREQ-05" "McpDbContext entities: TODOs, requirements and use cases (5 of 8)" "EF Core entity model, McpDbContext, area TodoRequirements. Columns are element properties." {
    include entRequirementTraceabilityLink entUseCaseFrLink entWorkspace entRequirement entUseCase
    autolayout lr 100 100
}
custom "EF-TODOREQ-06" "McpDbContext entities: TODOs, requirements and use cases (6 of 8)" "EF Core entity model, McpDbContext, area TodoRequirements. Columns are element properties." {
    include entRequirementsRecoveryRun entRequirementScopeLayer entUseCase entWorkspace
    autolayout lr 100 100
}
custom "EF-TODOREQ-07" "McpDbContext entities: TODOs, requirements and use cases (7 of 8)" "EF Core entity model, McpDbContext, area TodoRequirements. Columns are element properties." {
    include entUseCaseActor entUseCaseExtensionPoint entWorkspace entActor entUseCase
    autolayout lr 100 100
}
custom "EF-TODOREQ-08" "McpDbContext entities: TODOs, requirements and use cases (8 of 8)" "EF Core entity model, McpDbContext, area TodoRequirements. Columns are element properties." {
    include entUseCaseFlow entUseCaseSpecialRequirement entUseCaseStep entActor entWorkspace entUseCase
    autolayout tb 100 100
}
custom "EF-MEMORY-01" "McpDbContext entities: memory and graph (1 of 2)" "EF Core entity model, McpDbContext, area MemoryGraph. Columns are element properties." {
    include entMemory entMemoryVersion entMemoryIndex entMemoryEdge entWorkspace
    autolayout lr 100 100
}
custom "EF-MEMORY-02" "McpDbContext entities: memory and graph (2 of 2)" "EF Core entity model, McpDbContext, area MemoryGraph. Columns are element properties." {
    include entGraphEntity entGraphRelationship entWorkspace
    autolayout lr 100 100
}
custom "EF-TRIAGE-01" "McpDbContext entities: triage, federation, handoff and hostile review (1 of 5)" "EF Core entity model, McpDbContext, area TriageFederation. Columns are element properties." {
    include entTriageGroup entTriageReport entTriageResearchRun entTriageReportListItem entWorkspace
    autolayout tb 100 100
}
custom "EF-TRIAGE-02" "McpDbContext entities: triage, federation, handoff and hostile review (2 of 5)" "EF Core entity model, McpDbContext, area TriageFederation. Columns are element properties." {
    include entFederationProxy entFederationConflict entFederationOperation entFederationOutbox
    autolayout lr 100 100
}
custom "EF-TRIAGE-03" "McpDbContext entities: triage, federation, handoff and hostile review (3 of 5)" "EF Core entity model, McpDbContext, area TriageFederation. Columns are element properties." {
    include entFederationWorkspace entHandoffIngestionRun entHandoffDiagnostic entWorkspace entFederationProxy
    autolayout lr 100 100
}
custom "EF-TRIAGE-04" "McpDbContext entities: triage, federation, handoff and hostile review (4 of 5)" "EF Core entity model, McpDbContext, area TriageFederation. Columns are element properties." {
    include entHostileReviewRequest entHostileReviewArtifactLink entHostileReviewExecution entWorkspace
    autolayout lr 100 100
}
custom "EF-TRIAGE-05" "McpDbContext entities: triage, federation, handoff and hostile review (5 of 5)" "EF Core entity model, McpDbContext, area TriageFederation. Columns are element properties." {
    include entHostileReviewFinding entHostileReviewDiagnostic entWorkspace entHostileReviewRequest
    autolayout lr 100 100
}
custom "EF-IDENTITY-01" "McpIdentityDbContext entities: ASP.NET Core Identity (1 of 2)" "EF Core entity model, McpIdentityDbContext, area Identity. Columns are element properties." {
    include entMcpUser entIdentityUserRole entIdentityUserClaim entIdentityUserLogin entIdentityRole
    autolayout lr 100 100
}
custom "EF-IDENTITY-02" "McpIdentityDbContext entities: ASP.NET Core Identity (2 of 2)" "EF Core entity model, McpIdentityDbContext, area Identity. Columns are element properties." {
    include entIdentityUserToken entIdentityRole entIdentityRoleClaim entMcpUser
    autolayout lr 100 100
}
custom "EF-TXSEC-01" "TransactionSecurityDbContext entities: transaction security (1 of 3)" "EF Core entity model, TransactionSecurityDbContext, area TransactionSecurity. Columns are element properties." {
    include entKeyServerParty entKeyServerPartyKey entKeyServerSequence entKeyServerNonce
    autolayout lr 100 100
}
custom "EF-TXSEC-02" "TransactionSecurityDbContext entities: transaction security (2 of 3)" "EF Core entity model, TransactionSecurityDbContext, area TransactionSecurity. Columns are element properties." {
    include entKeyServerManifest entSubscriberTransaction entSubscriberSequence
    autolayout lr 100 100
}
custom "EF-TXSEC-03" "TransactionSecurityDbContext entities: transaction security (3 of 3)" "EF Core entity model, TransactionSecurityDbContext, area TransactionSecurity. Columns are element properties." {
    include entSubscriberNonce entTransactionAudit entTransactionPubSubMessage
    autolayout lr 100 100
}
