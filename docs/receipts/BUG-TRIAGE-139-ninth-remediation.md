# BUG-TRIAGE-139 Ninth Remediation Evidence

- Workspace: `F:\GitHub\McpServer\.mcpServer\worktrees\bug-triage-139-remediation`
- Base: `298c5fde3d1438ff7741ebec82ced796b207433e`
- Ninth-review starting HEAD: `43926e35b47ab6ce88ad4f98c849967ebbdb430d`
- Ninth review narrative: the ninth independent review narrative was not retained at its former temporary path; no exact repository-owned copy or SHA-256 is claimed. The strongest durable ninth evidence is the committed raw RED/GREEN artifacts referenced below.
- Process: BDPv4 RED -> GREEN -> REFACTOR
- TODO: `BUG-TRIAGE-139` remains open with `done: false`.
- Tooling: every repository, build, test, git, plugin, TODO, and evidence operation used PowerShell.Mcp with pwsh-compatible commands.

## RED chronology and raw artifacts

The original ninth test-only boundary was committed before production work. The audit reconstruction now exposes both eighth and ninth slices as separate test-only RED, raw-artifact, production GREEN, and evidence commits.

### Eighth reconstructed RED

Command:

```powershell
dotnet test tests/McpServer.Support.Mcp.Tests/McpServer.Support.Mcp.Tests.csproj -c Debug --no-build --no-restore --filter 'FullyQualifiedName~WorkspaceIdentityEighthAdversarialReviewTests|FullyQualifiedName~TransactionGatedRequirementsDocumentServiceEighthTests|FullyQualifiedName~WorkspaceIdentityLookupEighthProviderTests' --logger 'console;verbosity=normal' --logger 'trx;LogFileName=eighth-red-focused.trx' -- RunConfiguration.MaxCpuCount=1
```

Result: 9 executed, 2 passed, 7 failed, 0 skipped.

Failures:

- `WorkspaceIdentityEighthAdversarialReviewTests.IsWithinRoot_ExistingFileSymbolicLinks_ResolvePhysicalContainment`
- `WorkspaceIdentityLookupEighthProviderTests.WorkspaceService_GetAsync_UsesBoundedIdentityHashCandidates`
- `WorkspaceIdentityEighthAdversarialReviewTests.IsWithinRoot_ExistingInRootRegularFile_ReturnsTrue`
- `TransactionGatedRequirementsDocumentServiceEighthTests.GenerateAllAsync_WhenCommitRejectsAfterExistingFileWrite_RestoresOriginalContent`
- `TransactionGatedRequirementsDocumentServiceEighthTests.GenerateAllAsync_ExistingInRootFile_CommitsExport`
- `WorkspaceIdentityLookupEighthProviderTests.FederationTopology_RegisterWorkspace_FiltersCandidatesServerSide`
- `WorkspaceIdentityLookupEighthProviderTests.FederationTopology_PhysicalAliasLookup_MaterializesOnlyIdentityHashCandidate`

Raw evidence: `docs/receipts/artifacts/BUG-TRIAGE-139/eighth-red/`.

The immediately following production commit runs the same scope 9/9 green. Raw GREEN evidence is under `docs/receipts/artifacts/BUG-TRIAGE-139/eighth-green/`.

### Ninth reconstructed RED

Original consumer/provider RED results:

- Support/SQLite: 14 executed, 1 passed, 13 failed, 0 skipped.
- Marker trust: 3 executed, 1 passed, 2 failed, 0 skipped.
- SQL Server LocalDB migration: 1 executed, 1 failed, 0 skipped.
- PostgreSQL migration: 1 executed, 1 failed, 0 skipped.
- Adjacent bounded federation preflight: 1 executed, 1 failed, 0 skipped; expected 4 ordered batches, actual 0.

Support/SQLite failures:

- `RequirementsWikiNinthAdversarialReviewTests.RequirementsWikiWriter_NestedDirectorySymlinkOutsideRoot_DoesNotDeleteExternalStaleFile`
- `RequirementsWikiNinthAdversarialReviewTests.RequirementsWikiWriter_NestedWindowsJunctionOutsideRoot_DoesNotDeleteExternalStaleFile`
- `SqliteWorkspaceIdentityNinthProviderTests.FederationWorkspaceLookups_MatureDatabase_UseCompositeIdentitySearchPlans`
- `UseCaseRequestIdNinthAdversarialReviewTests.CreateUseCase_UnmatchedSurrogateRequestId_ReturnsTypedValidationWithoutPersistence`
- `RequirementsWikiNinthAdversarialReviewTests.GenerateWikiAsync_Rollback_DoesNotWriteExternalFileThroughSymlink`
- `SqliteWorkspaceIdentityNinthProviderTests.IndexedIdentityMigration_ApplyBackfillDowngradeAndReapply`
- `UseCaseRequestIdNinthAdversarialReviewTests.WorkspaceIdentity_DistinctUnmatchedSurrogateRequestIds_AreRejectedBeforeHashing`
- `RequirementsWikiNinthAdversarialReviewTests.GenerateWikiAsync_RollbackDirectoryCleanup_DoesNotDeleteExternalEmptyDirectory`
- `RequirementsWikiNinthAdversarialReviewTests.GenerateWikiAsync_NestedDirectorySymlinkOutsideRoot_DoesNotReadExternalSnapshot`
- `SqliteWorkspaceIdentityNinthProviderTests.LegacyBackfill_OverBatchSize_UsesBoundedSetBasedBatchesThenNoOps`
- `SqliteWorkspaceIdentityNinthProviderTests.UseCaseCreationWorkspaceLookup_MatureDatabase_UsesOnlyIndexBackedSearchPlans`
- `SqliteWorkspaceIdentityNinthProviderTests.WorkspaceServiceLookup_MatureDatabase_UsesOnlyIndexBackedSearchPlans`
- `SqliteWorkspaceIdentityNinthProviderTests.MatureHydratedStartup_PerformsNoIdentityBackfillReadsOrUpdates`

Other failures:

- `MarkerFileTrustNinthTests.TryLoadTrustedMarker_TamperedUncWorkspace_PerformsZeroPhysicalComparisonsBeforeHmac`
- `MarkerFileTrustNinthTests.TryResolveWithDiagnostics_TamperedUncWorkspace_PerformsZeroPhysicalComparisonsBeforeHmac`
- `SqlServerWorkspaceIdentityNinthProviderTests.IndexedIdentityMigration_ApplyBackfillDowngradeAndReapply`
- `PostgreSqlWorkspaceIdentityNinthProviderTests.IndexedIdentityMigration_ApplyBackfillDowngradeAndReapply`
- `SqliteWorkspaceIdentityNinthProviderTests.IndexedIdentityMigration_LegacyFederationPreflightReadsBoundedBatches`

Raw evidence: `docs/receipts/artifacts/BUG-TRIAGE-139/ninth-red/`. A reviewer can check out the test-only RED commit and run the named filters; the artifact commit is its immediate child and still has the failing production tree.

## Remediation

- Requirements wiki snapshot, writer stale cleanup, rollback reads/writes, and rollback directory cleanup now share `WorkspaceContainedFileSystem`. Traversal never follows directory reparse points, physically validates file leaves, revalidates immediately before mutation, fails closed on inaccessible/unresolved entries, and retains valid ordinary nested directories.
- Both trusted marker entry points authenticate the HMAC before cache comparison or any physical workspace-path operation. Tampered UNC/network-like marker values cause zero physical-comparer calls. Trusted cache rotation/timestamps and valid UNC values remain supported.
- Workspace registration/lookup and use-case creation perform ordered primary-key then `NormalizedIdentityHash` probes. Federation registration performs one composite `ProxyIdentityHash` + `WorkspaceIdentityHash` probe. Hot paths contain no client-side table scan, SQL `OR`, `ToUpper`, or legacy non-sargable predicate.
- Migration `20260817140000_BugTriage139IndexedIdentityLookups` is isolated in SQLite, SQL Server, and PostgreSQL migration assemblies. It adds only two fixed 64-character federation hashes and the provider-safe composite unique index. Forward, legacy backfill, downgrade, and reapply pass on all providers.
- Startup workspace, receipt, and federation repairs use ordered batches of 256 and set-based CASE updates. The one-time federation collision preflight also reads ordered provider-correct batches. A fully hydrated second startup materializes or updates no derived-identity rows.
- The receipt upgrade path rehashes all legacy request keys only when the v2 receipt migration is pending, preserving delimiter-collision compatibility while mature startups remain bounded.
- Request-key producers reject unmatched UTF-16 surrogate code units before hashing. Service, REST, MCP, and client surfaces retain typed, sanitized validation contracts.
- The Windows junction test executes a real junction on Windows; the same test executes a real native symbolic link on POSIX. There are no skips or silent platform returns.

## Index/query-plan evidence

SQLite mature-data tests execute `EXPLAIN QUERY PLAN` for every captured hot lookup and reject `SCAN`:

- exact workspace probe: `SEARCH` by the workspace primary-key index;
- canonical workspace/use-case probe: `SEARCH` by `IX_Workspaces_NormalizedIdentityHash`;
- federation probe: `SEARCH` by `IX_FederationWorkspaces_ProxyIdentityHash_WorkspaceIdentityHash`.

The provider translation tests assert SQL Server and PostgreSQL emit two equality predicates with no `OR` and no case-transform function. The live provider migration tests prove the corresponding schema snapshot, backfill, downgrade, and reapply.

## GREEN validation

Authoritative serialized UseCase command:

```powershell
dotnet test tests/McpServer.Support.Mcp.Tests/McpServer.Support.Mcp.Tests.csproj -c Release --no-build --no-restore --settings docs/receipts/artifacts/BUG-TRIAGE-139/ninth-green/serial.runsettings --filter 'FullyQualifiedName~UseCase' --logger 'console;verbosity=minimal' --logger 'trx;LogFileName=usecase-authoritative.trx'
```

Discovery and execution were performed by one serialized PowerShell.Mcp pipeline with the same project, filter, build, and runsettings:

- discovered: 198;
- executed/TRX result nodes: 198;
- passed: 198;
- failed: 0;
- skipped/not executed: 0.

The prior 194-discovered/193-executed discrepancy was the separate-invocation omission of `PostgreSqlUseCaseFifthProviderTests.SameRequestContention_ReturnsOneFullyPopulatedPayload`. The authoritative combined invocation now discovers and executes the complete same scope with equal counts. No skip metadata or production exclusion was introduced.

Other final gates:

- requirements wiki/transaction/renderer: 54/54, 0 skipped;
- marker resolver/cache/trust: 18/18, 0 skipped;
- federation/GraphRAG identity compatibility: 5/5, 0 skipped;
- reconstructed ninth consumer/SQLite focused scope: 15/15, 0 skipped;
- reconstructed marker RED-to-GREEN scope: 3/3, 0 skipped;
- ninth SQLite/SQL Server/PostgreSQL migration/query-plan/translation/startup matrix: 11/11, 0 skipped;
- REST/MCP contracts: 17/17, 0 skipped;
- client UseCase contracts: 12/12, 0 skipped;
- SQLite UseCase provider matrix: 17/17, 0 skipped;
- SQL Server LocalDB UseCase provider matrix: 14/14, 0 skipped;
- PostgreSQL UseCase provider matrix: 15/15, 0 skipped.

Raw comprehensive GREEN evidence: `docs/receipts/artifacts/BUG-TRIAGE-139/ninth-green/`. Reconstructed immediate-child GREEN evidence: `docs/receipts/artifacts/BUG-TRIAGE-139/ninth-green-reconstructed/`.

Twelve projects were built in Release with `--no-restore -warnaserror`: Client, Repl.Host, Storage, Services, GraphRag, Support.Mcp, all three provider migration projects, Support.Mcp.Tests, Repl.IntegrationTests, and Client.Tests. Every build reported 0 warnings and 0 errors.

`git diff --check` passed. Normal and `--ignore-space-at-eol` numstats were identical for all modified pre-existing files.

Historical approved migrations were not changed merely to increase coverage. The new isolated migration and the coordinator's causally necessary legacy receipt/backfill logic are the only migration-related ninth changes. LocalDB ran serially without deleting or disrupting unrelated instances.

## Audit reconstruction

Backup refs:

- existing pre-trailer backup: `refs/heads/backup/bug-triage-139-pre-trailer-rewrite-20260817-1030`;
- pre-ninth reconstruction: `refs/backup/bug-triage-139-before-ninth-reconstruction-20260817` -> `f7bd6ad61cff8503f8a7bd74d3b9750c6c10d40b`.

Reconstructed commits:

- eighth test-only RED: `af361e0a54bc01f81252aa40760db9c8d7a8d54c`;
- eighth raw RED artifact: `10327c48270c565d08b259f19f38d2d87b5d2eec`;
- eighth production GREEN: `b3eef84b31d084d9628c6fc05808179ac2aa2541`;
- eighth evidence: `769b0c5b3668614471d29b6591bde493e68047be`;
- ninth test-only RED: `7dd3d0a397bd00980ec2266bc8782f25268a70fb`;
- ninth raw RED artifact: `92d68adc2a1970e0931d388750864d58a06a999b`;
- ninth production GREEN: `6f31e848bcb7ba34789324cd650df19bc2bc8ceb`;
- ninth evidence: this receipt commit.

Old-to-new map:

- `027ab7f931fac9172fa54306211c96fec342cf0e` -> `b3eef84b31d084d9628c6fc05808179ac2aa2541` (production), with new predecessors `af361e0a...` and `10327c48...`;
- `43926e35b47ab6ce88ad4f98c849967ebbdb430d` -> `769b0c5b3668614471d29b6591bde493e68047be`;
- `120f6d5e0eb085817a7a3eb9c0d4095abbda88f8` -> `7dd3d0a397bd00980ec2266bc8782f25268a70fb` plus `92d68adc2a1970e0931d388750864d58a06a999b`;
- `f7bd6ad61cff8503f8a7bd74d3b9750c6c10d40b` -> `6f31e848bcb7ba34789324cd650df19bc2bc8ceb`.

The source and test tree at reconstructed production commit `6f31e848...` is byte-for-byte Git-equivalent to validated pre-reconstruction commit `f7bd6ad6...` when audit receipts are excluded. Reconstruction used a separate worktree/ref and the feature ref is replaced only after final tree and parent/trailer verification.

Every commit in base..HEAD has an `AI-Signature` and `AI-Confidence` trailer. Reconstructed timestamps are current reconstruction timestamps; no historical timestamp was fabricated.

## Changed code/test files

- `src/McpServer.Repl.Host/MarkerFileClientOptionsResolver.cs`
- `src/McpServer.Services/Requirements/RequirementsWikiDocumentRenderer.cs`
- `src/McpServer.Services/Services/FederationTopologyService.cs`
- `src/McpServer.Services/Services/WorkspaceService.cs`
- `src/McpServer.Services/UseCases/UseCaseCqrsHelpers.cs`
- `src/McpServer.Services/UseCases/UseCaseCreation.cs`
- `src/McpServer.Storage/Database/McpDatabaseMigrationCoordinator.cs`
- `src/McpServer.Storage/Entities/FederationWorkspaceEntity.cs`
- `src/McpServer.Storage/McpDbContext.cs`
- `src/McpServer.Storage/WorkspaceContainedFileSystem.cs`
- `src/McpServer.Storage/WorkspaceIdentity.cs`
- `src/McpServer.Support.Mcp/Services/TransactionGatedRequirementsDocumentService.cs`
- `src/McpServer.Storage.SqliteMigrations/Migrations/20260817140000_BugTriage139IndexedIdentityLookups.cs`
- `src/McpServer.Storage.SqliteMigrations/Migrations/20260817140000_BugTriage139IndexedIdentityLookups.Designer.cs`
- `src/McpServer.Storage.SqliteMigrations/Migrations/McpDbContextModelSnapshot.cs`
- `src/McpServer.Storage.SqlServerMigrations/Migrations/20260817140000_BugTriage139IndexedIdentityLookups.cs`
- `src/McpServer.Storage.SqlServerMigrations/Migrations/20260817140000_BugTriage139IndexedIdentityLookups.Designer.cs`
- `src/McpServer.Storage.SqlServerMigrations/Migrations/McpDbContextModelSnapshot.cs`
- `src/McpServer.Storage.PostgreSqlMigrations/Migrations/20260817140000_BugTriage139IndexedIdentityLookups.cs`
- `src/McpServer.Storage.PostgreSqlMigrations/Migrations/20260817140000_BugTriage139IndexedIdentityLookups.Designer.cs`
- `src/McpServer.Storage.PostgreSqlMigrations/Migrations/McpDbContextModelSnapshot.cs`
- `tests/McpServer.Repl.IntegrationTests/MarkerFileTrustNinthTests.cs`
- `tests/McpServer.Support.Mcp.Tests/Services/RequirementsWikiNinthAdversarialReviewTests.cs`
- `tests/McpServer.Support.Mcp.Tests/Services/UseCaseRequestIdNinthAdversarialReviewTests.cs`
- `tests/McpServer.Support.Mcp.Tests/Services/WorkspaceIdentityLookupEighthProviderTests.cs`
- `tests/McpServer.Support.Mcp.Tests/Storage/WorkspaceIdentityNinthProviderTests.cs`

Supported TODO update `workflow.todo.update` succeeded with request `req-20260817T151931Z-5737`; the returned item remained `done: false`.

No merge or push was performed. Independent approval remains pending.
