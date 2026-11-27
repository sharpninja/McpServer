# TEST-MCP-WIKIEXPORT

[Back to Testing Requirements](Testing-Requirements)

## TEST-MCP-WIKIEXPORT

### TEST-MCP-WIKIEXPORT-001

Tests must cover docs/wiki.yaml loading, validation, renderer output, service integration, unchanged default behavior, and BDPv4 traceability for configured GitHub and Azure wiki exports.

**Acceptance Criteria:**
- [x] Loader tests cover valid schema, absent optional home template, invalid schema, duplicate ids, duplicate targets, bad platforms, missing source files, invalid generated sources, path traversal, and navigation references to unknown or duplicated documents.
- [x] Service tests prove no docs/wiki.yaml preserves current GitHub and Azure wiki output.
- [x] Export tests prove configured workspace Markdown and generated sources are emitted to eligible GitHub and Azure folders with correct platform filtering.
- [x] Navigation tests prove GitHub _Sidebar.md and Azure .order files follow the configured tree including nested sections.
- [x] Home tests prove template token replacement and default navigation-derived home rendering.
- [x] Invalid configuration tests prove no existing export files are modified on validation failure.
- [x] Build guidance or traceability tests prove generated manifests include configured documents and preserve required requirements documents when declared.
- [ ] An optional workspace Markdown footer template supplies exactly one visible footer per exported content page, including generated, static, DocFX and group pages. GitHub uses its managed footer and the legacy format renders it inline. Sidecars and non-page assets are preserved. The shipped default credits QBrain and links to https://github.com/sharpninja/McpServer.
- [ ] An absent footer setting preserves the legacy layout and uses the QBrain attribution in the GitHub footer. A configured missing, empty, non-Markdown, escaping, or delimiter-conflicting template fails before any export file changes.
- [ ] Repeated exports produce one footer without changing inputs, publication Home keeps the footer after documentation links, and wiki imports preserve canonical records, acceptance criteria and mappings even when footer text resembles requirement content.
- [ ] When enabled, deterministic, case-insensitive grouping is based on whole leading ID segments before the first digit-leading segment within each requirement kind. At least two distinct IDs are required. Invalid prefixes and singletons remain on canonical pages.
- [ ] Each qualifying group has one safe read-only child page per eligible platform, one parent link and a link back. Canonical parent filenames, original member records and existing anchors are retained.
- [ ] Manifests, navigation, ZIP and cleanup consume the same platform inventory. Collisions fail before writes and repeat exports remove obsolete group pages and links. Absent or disabled grouping preserves the existing layout.
- [ ] Group pages render full typed member records and criteria with the same mapping context as the parent. Group artifacts never supply imported requirements or affect source selection when canonical inputs and timestamps are unchanged.

### TEST-MCP-WIKIEXPORT-002

Tests must prove marker generation creates a valid default docs/wiki.yaml, preserves existing configs, and keeps marker generation behavior intact.

**Acceptance Criteria:**
- [x] A marker write in a workspace without docs/wiki.yaml creates docs/wiki.yaml and the marker file.
- [x] The generated docs/wiki.yaml deserializes to an object with schema mcp-wiki-export/v1, six declared generated documents, and navigation references covering every document once.
- [x] A marker write in a workspace with an existing docs/wiki.yaml preserves the exact existing content.
- [x] Focused marker and wiki export tests pass with zero failures and zero skips.

### TEST-MCP-WIKIEXPORT-003

WikiExport_WithoutDumpFlag_UnchangedBehavior; WikiExport_WithDumpFlag_WritesVersionedJsonKeyedByWorkspace; Dump_ContainsTodoRowsAndRequirementLinksMatchingStore; Dump_Sha256_MatchesCanonicalUtf8Json; DumpPolicyRegistry_CoversEveryCurrentDbSetExactlyOnce; Dump_SecretSentinels_RedactedAcrossScalarNestedAndEncodedPayloads; Dump_Diagnostics_NeverEchoSecretValue; Dump_ExcludesUnrelatedWorkspaceAndGlobalSecrets; Dump_FinalByteHash_VerifiesAfterRedaction; Dump_RelationshipsRemainLosslessExceptDeclaredRedactedText; Import_SourceOwnedProduct_RemapOwnerToDestination; Import_SharedProductMissing_RequiresSharedProductMapping; Import_SharedProductEquivalent_ReusesAuthorizedDestinationProduct; Dump_ForeignOwnedProduct_ExportsOnlySourceWorkspaceMembership.

**Acceptance Criteria:**
- [x] Named tests exist: WikiExport_WithoutDumpFlag_UnchangedBehavior; WikiExport_WithDumpFlag_WritesVersionedJsonKeyedByWorkspace; Dump_ContainsTodoRowsAndRequirementLinksMatchingStore; Dump_Sha256_MatchesCanonicalUtf8Json. (evidence: tests/McpServer.Support.Mcp.Tests/Services/WikiDumpG6OverlayTests.cs; G8 SHA-256 B652C283B446F2B82832665499811741B71F3C63D29F56E30181158829C596A6)
- [x] Named tests exist: WikiExport_WithoutDumpFlag_UnchangedBehavior; WikiExport_WithDumpFlag_WritesVersionedJsonKeyedByWorkspace; Dump_ContainsTodoRowsAndRequirementLinksMatchingStore; Dump_Sha256_MatchesCanonicalUtf8Json; DumpPolicyRegistry_CoversEveryCurrentDbSetExactlyOnce; Dump_SecretSentinels_RedactedAcrossScalarNestedAndEncodedPayloads; Dump_Diagnostics_NeverEchoSecretValue; Dump_ExcludesUnrelatedWorkspaceAndGlobalSecrets; Dump_FinalByteHash_VerifiesAfterRedaction; Dump_RelationshipsRemainLosslessExceptDeclaredRedactedText; Import_SourceOwnedProduct_RemapOwnerToDestination; Import_SharedProductMissing_RequiresSharedProductMapping; Import_SharedProductEquivalent_ReusesAuthorizedDestinationProduct; Dump_ForeignOwnedProduct_ExportsOnlySourceWorkspaceMembership. (evidence: tests/McpServer.Support.Mcp.Tests/Services/WikiDumpG6OverlayTests.cs; G8 SHA-256 B652C283B446F2B82832665499811741B71F3C63D29F56E30181158829C596A6)

### TEST-MCP-WIKIEXPORT-004

AddWorkspace_DumpParam_HydratesTodosFromDumpNotTodoYaml; Import_OperationalColumnsContainOnlyDestinationIdsAndPaths; Import_MalformedDump_VersionMismatch_MissingTables_UnsafePath_Rejected; Import_IdempotentReimport_NoDuplicateTodos. Import_ImmutableSourceProvenancePreservesOriginalIdentity; Import_ReplayableFederationAgentTriageHandoffRows_AreInertAfterRestart; Import_CredentialedFeatures_RequireDestinationReconfiguration; Import_RelationshipsAndEntityMaps_AreComplete; AddWorkspace_DumpReplay_CheckedBeforeDuplicateRegistration; Import_RegistrationAndHydration_CommitInOneTransaction; Import_PreCommitFailureOrCancellation_RollsBackWorkspaceAndRows; Import_PostCommitProjectionInitOrStartFailure_IsRecoverable; Import_CancelAfterCommit_LeavesPendingActivation; Import_ExistingRepositoryCheckout_PreservesGitAndSourceFiles; Import_ActivatedReplay_DoesNotReapplyFreshFilesystemPreconditions; Import_ServiceOwnedPathConflict_PrecommitDiagnosticOrRecoverableActivationFailure; Import_UnknownExistingMarker_IsNeverReplaced; Import_ConcurrentSameDump_SingleCommitAndReplayReceipt; Import_ConflictingDump_RejectsWithoutPartialHydration.

**Acceptance Criteria:**
- [x] Named tests exist: AddWorkspace_DumpParam_HydratesTodosFromDumpNotTodoYaml; Import_OperationalColumnsContainOnlyDestinationIdsAndPaths; Import_MalformedDump_VersionMismatch_MissingTables_UnsafePath_Rejected; Import_IdempotentReimport_NoDuplicateTodos. (evidence: tests/McpServer.Support.Mcp.Tests/Services/WikiDumpG6OverlayTests.cs; G8 SHA-256 B652C283B446F2B82832665499811741B71F3C63D29F56E30181158829C596A6)
- [x] Named tests exist: AddWorkspace_DumpParam_HydratesTodosFromDumpNotTodoYaml; Import_OperationalColumnsContainOnlyDestinationIdsAndPaths; Import_MalformedDump_VersionMismatch_MissingTables_UnsafePath_Rejected; Import_IdempotentReimport_NoDuplicateTodos. Import_ImmutableSourceProvenancePreservesOriginalIdentity; Import_ReplayableFederationAgentTriageHandoffRows_AreInertAfterRestart; Import_CredentialedFeatures_RequireDestinationReconfiguration; Import_RelationshipsAndEntityMaps_AreComplete; AddWorkspace_DumpReplay_CheckedBeforeDuplicateRegistration; Import_RegistrationAndHydration_CommitInOneTransaction; Import_PreCommitFailureOrCancellation_RollsBackWorkspaceAndRows; Import_PostCommitProjectionInitOrStartFailure_IsRecoverable; Import_CancelAfterCommit_LeavesPendingActivation; Import_ExistingRepositoryCheckout_PreservesGitAndSourceFiles; Import_ActivatedReplay_DoesNotReapplyFreshFilesystemPreconditions; Import_ServiceOwnedPathConflict_PrecommitDiagnosticOrRecoverableActivationFailure; Import_UnknownExistingMarker_IsNeverReplaced; Import_ConcurrentSameDump_SingleCommitAndReplayReceipt; Import_ConflictingDump_RejectsWithoutPartialHydration. (evidence: tests/McpServer.Support.Mcp.Tests/Services/WikiDumpG6OverlayTests.cs; G8 SHA-256 B652C283B446F2B82832665499811741B71F3C63D29F56E30181158829C596A6)

### TEST-MCP-WIKIEXPORT-005

TodoYaml_NotSourceOfTruth_WhenDumpPresent; TodoYaml_Cleanup_ArchivesWithEvidence_NoSilentDelete; DumpAndTodoYaml_Conflict_DumpWins_DiagnosticNamesBoth.

**Acceptance Criteria:**
- [x] Named tests exist: TodoYaml_NotSourceOfTruth_WhenDumpPresent; TodoYaml_Cleanup_ArchivesWithEvidence_NoSilentDelete; DumpAndTodoYaml_Conflict_DumpWins_DiagnosticNamesBoth. (evidence: tests/McpServer.Support.Mcp.Tests/Services/WikiDumpG6OverlayTests.cs; G8 SHA-256 B652C283B446F2B82832665499811741B71F3C63D29F56E30181158829C596A6)
