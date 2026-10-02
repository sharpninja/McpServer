# BUG-TRIAGE-139 tenth remediation receipt

Date: 2026-08-17
Branch: codex/bug-triage-139-remediation
Slice baseline: 3e37ff653bfc564241968fc52dac127728379529
Base: 298c5fde3d1438ff7741ebec82ced796b207433e
TODO state: open, done=false
Independent approval: pending

## Auditable chronology

- Test-only RED commit: `2ae411fe1e03fbf7ffdfda9fef7d5f09a6615be6`.
- Raw RED artifact commit, while production still matched the baseline: `7076255e94be6f185b975a504fc2597e9b26c637`.
- Production GREEN commit: `2e6c11add00825836c5233fe955a3caa2e4a5990`.
- This receipt and the GREEN artifacts are committed afterward as the evidence commit; the exact final evidence SHA is reported by `git rev-parse HEAD` and in the handoff response.
- No history was rewritten. No merge or push was performed.

## Exact independent-review narrative

The exact tenth review is repository-owned at:
`docs/receipts/artifacts/BUG-TRIAGE-139/reviews/tenth-independent-review.txt`

- Original PowerShell.Mcp output: `pwsh_output_20260817_110953_288_eg0prvwf.mls.txt`.
- Source and artifact length: 29,072 bytes.
- Source and artifact SHA-256: `8387693E535A670A82AF201646F659B010A1E49F7A76B52548EFD98FEC292D9E`.
- Copy was byte-preserving and the source/artifact hashes matched immediately afterward.
- The ninth temporary narrative was checked and no longer existed. It is not reconstructed or paraphrased as exact; the strongest ninth evidence remains its repository-owned RED/GREEN TRX, console artifacts, and ninth receipt.

## RED evidence

The RED commands, console streams, and TRX files are under:
`docs/receipts/artifacts/BUG-TRIAGE-139/tenth-red`.

Client RED:
- 18 executed.
- 1 passed.
- 17 failed.
- 0 skipped.

Exact client failures:
- `McpServer.Client.Tests.UseCaseClientTenthAdversarialReviewTests.CreateAsync_MalformedTextAnywhere_RejectsBeforeSerialization(field: "Postcondition")`
- `McpServer.Client.Tests.UseCaseClientTenthAdversarialReviewTests.CreateFromFrAsync_MalformedTextAnywhere_RejectsBeforeSerialization(field: "RequestId")`
- `McpServer.Client.Tests.UseCaseClientTenthAdversarialReviewTests.CreateFromFrAsync_MalformedTextAnywhere_RejectsBeforeSerialization(field: "FrId")`
- `McpServer.Client.Tests.UseCaseClientTenthAdversarialReviewTests.CreateAsync_MalformedTextAnywhere_RejectsBeforeSerialization(field: "RequestId")`
- `McpServer.Client.Tests.UseCaseClientTenthAdversarialReviewTests.CreateFromFrAsync_MalformedTextAnywhere_RejectsBeforeSerialization(field: "BriefDescription")`
- `McpServer.Client.Tests.UseCaseClientTenthAdversarialReviewTests.CreateAsync_MalformedTextAnywhere_RejectsBeforeSerialization(field: "InitialSteps.DataEntities")`
- `McpServer.Client.Tests.UseCaseClientTenthAdversarialReviewTests.CreateAsync_MalformedTextAnywhere_RejectsBeforeSerialization(field: "InitialSteps.SystemResponse")`
- `McpServer.Client.Tests.UseCaseClientTenthAdversarialReviewTests.CreateAsync_MalformedTextAnywhere_RejectsBeforeSerialization(field: "Notes")`
- `McpServer.Client.Tests.UseCaseClientTenthAdversarialReviewTests.CreateAsync_MalformedTextAnywhere_RejectsBeforeSerialization(field: "Title")`
- `McpServer.Client.Tests.UseCaseClientTenthAdversarialReviewTests.CreateAsync_MalformedTextAnywhere_RejectsBeforeSerialization(field: "InitialSteps.Action")`
- `McpServer.Client.Tests.UseCaseClientTenthAdversarialReviewTests.CreateAsync_MalformedTextAnywhere_RejectsBeforeSerialization(field: "LinkType")`
- `McpServer.Client.Tests.UseCaseClientTenthAdversarialReviewTests.CreateFromFrAsync_MalformedTextAnywhere_RejectsBeforeSerialization(field: "Title")`
- `McpServer.Client.Tests.UseCaseClientTenthAdversarialReviewTests.CreateAsync_MalformedTextAnywhere_RejectsBeforeSerialization(field: "BriefDescription")`
- `McpServer.Client.Tests.UseCaseClientTenthAdversarialReviewTests.CreateAsync_DistinctMalformedPayloadsWithSameRequestId_NeverSerialize`
- `McpServer.Client.Tests.UseCaseClientTenthAdversarialReviewTests.CreateAsync_MalformedTextAnywhere_RejectsBeforeSerialization(field: "FrId")`
- `McpServer.Client.Tests.UseCaseClientTenthAdversarialReviewTests.CreateAsync_MalformedTextAnywhere_RejectsBeforeSerialization(field: "Precondition")`
- `McpServer.Client.Tests.UseCaseClientTenthAdversarialReviewTests.CreateAsync_MalformedTextAnywhere_RejectsBeforeSerialization(field: "Scope")`

Support/provider/artifact RED:
- 28 executed.
- 3 passed.
- 25 failed.
- 0 skipped.

Exact support/provider/artifact failures:
- `McpServer.Support.Mcp.Tests.Services.UseCaseTenthAdversarialReviewTests.CreateUseCase_MalformedTextAnywhere_ReturnsTypedValidationBeforePersistence(field: "FrId")`
- `McpServer.Support.Mcp.Tests.Storage.SqliteUseCaseTenthProviderTests.ReceiptReplay_CurrentHash_UsesSearchWithoutScanOrTemporarySort`
- `McpServer.Support.Mcp.Tests.Storage.PostgreSqlUseCaseTenthProviderTests.ReceiptLookupMigration_ApplyBackfillDowngradeAndReapply`
- `McpServer.Support.Mcp.Tests.Services.UseCaseTenthAdversarialReviewTests.CreateUseCase_MalformedTextAnywhere_ReturnsTypedValidationBeforePersistence(field: "BriefDescription")`
- `McpServer.Support.Mcp.Tests.Services.UseCaseTenthAdversarialReviewTests.CreateFromFr_MalformedTextAnywhere_ReturnsTypedValidationBeforePersistence(field: "Title")`
- `McpServer.Support.Mcp.Tests.Storage.SqliteUseCaseTenthProviderTests.ReceiptReplay_NullHash_UsesCompositeSearchWithoutOrOrTemporarySort`
- `McpServer.Support.Mcp.Tests.Storage.SqliteUseCaseTenthProviderTests.LegacyCollisionPreflight_OverBatchSize_UsesBoundedReadsAndStaging`
- `McpServer.Support.Mcp.Tests.Storage.SqlServerUseCaseTenthProviderTests.ReceiptProbeTranslation_UsesThreeSargableQueries`
- `McpServer.Support.Mcp.Tests.Services.UseCaseTenthAdversarialReviewTests.CreateUseCase_MalformedTextAnywhere_ReturnsTypedValidationBeforePersistence(field: "Notes")`
- `McpServer.Support.Mcp.Tests.Services.UseCaseTenthAdversarialReviewTests.CreateUseCase_MalformedTextAnywhere_ReturnsTypedValidationBeforePersistence(field: "InitialSteps.Action")`
- `McpServer.Support.Mcp.Tests.Storage.SqliteUseCaseTenthProviderTests.ReceiptReplay_LegacyHash_UsesSplitSearchWithoutOrOrTemporarySort`
- `McpServer.Support.Mcp.Tests.Services.UseCaseTenthAdversarialReviewTests.CreateUseCase_MalformedTextAnywhere_ReturnsTypedValidationBeforePersistence(field: "Title")`
- `McpServer.Support.Mcp.Tests.Services.UseCaseTenthAdversarialReviewTests.CreateUseCase_MalformedTextAnywhere_ReturnsTypedValidationBeforePersistence(field: "Postcondition")`
- `McpServer.Support.Mcp.Tests.Services.UseCaseTenthAdversarialReviewTests.CreateUseCase_MalformedTextAnywhere_ReturnsTypedValidationBeforePersistence(field: "Precondition")`
- `McpServer.Support.Mcp.Tests.Storage.PostgreSqlUseCaseTenthProviderTests.ReceiptProbeTranslation_UsesThreeSargableQueries`
- `McpServer.Support.Mcp.Tests.Storage.SqlServerUseCaseTenthProviderTests.ReceiptLookupMigration_ApplyBackfillDowngradeAndReapply`
- `McpServer.Support.Mcp.Tests.Services.UseCaseTenthAdversarialReviewTests.CreateUseCase_MalformedTextAnywhere_ReturnsTypedValidationBeforePersistence(field: "LinkType")`
- `McpServer.Support.Mcp.Tests.Services.UseCaseTenthAdversarialReviewTests.CreateFromFr_MalformedTextAnywhere_ReturnsTypedValidationBeforePersistence(field: "FrId")`
- `McpServer.Support.Mcp.Tests.Services.UseCaseTenthAdversarialReviewTests.CreateFromFr_MalformedTextAnywhere_ReturnsTypedValidationBeforePersistence(field: "BriefDescription")`
- `McpServer.Support.Mcp.Tests.Services.UseCaseTenthAdversarialReviewTests.CreateUseCase_MalformedTextAnywhere_ReturnsTypedValidationBeforePersistence(field: "InitialSteps.SystemResponse")`
- `McpServer.Support.Mcp.Tests.Services.UseCaseTenthAdversarialReviewTests.CreateUseCase_MalformedTextAnywhere_ReturnsTypedValidationBeforePersistence(field: "InitialSteps.DataEntities")`
- `McpServer.Support.Mcp.Tests.Services.UseCaseTenthAdversarialReviewTests.CreateUseCase_DistinctMalformedPayloadsWithSameRequestId_NeverReplay`
- `McpServer.Support.Mcp.Tests.Documentation.BugTriage139ReviewArtifactTests.NinthReceipt_RecordsMissingNarrativeWithoutTemporaryDependency`
- `McpServer.Support.Mcp.Tests.Services.UseCaseTenthAdversarialReviewTests.CreateUseCase_MalformedTextAnywhere_ReturnsTypedValidationBeforePersistence(field: "Scope")`
- `McpServer.Support.Mcp.Tests.Documentation.BugTriage139ReviewArtifactTests.TenthReviewNarrative_IsRepositoryOwnedAndMatchesProvenanceHash`

Combined RED:
- 46 executed.
- 4 passed.
- 42 failed.
- 0 skipped.

## Remediation

### Complete well-formed UTF-16 contract

- Added one shared public request-graph validator in the client contract assembly.
- The walker covers request ID, title, description, scope, pre/postconditions, notes, FR ID/link values, and every nested step string.
- Client create and create-from-FR validate before JSON serialization.
- Sync/async server creation and every request-key producer validate before workspace access, hashing, persistence, or replay.
- Malformed text returns the established typed, sanitized validation result.
- Distinct malformed requests sharing one RequestId cannot be accepted as an exact replay.
- Valid Unicode behavior remains unchanged.

### Split indexed legacy receipt probes

- Replaced the legacy OR/null query with ordered current-hash, legacy-hash, and null-hash probes.
- Every probe is bounded and independently cancellable.
- Added fixed 64-character raw workspace/request identity hashes and a provider-safe composite lookup index.
- The PostgreSQL physical index name is shortened to `IX_UseCases_CreationReceiptLookup`; SQLite and SQL Server use the default model name.
- SQLite EXPLAIN tests prove SEARCH and reject SCAN, OR, MULTI-INDEX OR, and TEMP B-TREE.
- SQL Server and PostgreSQL translation tests prove three separate sargable queries with no OR/case transform.
- Existing current, legacy, and null-hash receipts retain exact replay, identity validation, tamper/conflict handling, workspace isolation, and cancellation.

### Bounded legacy collision preflight

- Replaced whole-table workspace/receipt lists and client dictionaries with 256-row keyset reads.
- Candidates are staged through provider-specific temporary tables with bounded multi-row inserts.
- Collision detection is provider-side with GROUP BY; diagnostics remain deterministic.
- Real SQLite evidence used 513 legacy workspaces and 513 legacy receipts.
- The test observed exactly four workspace reads and four receipt reads.
- It observed at least three staging inserts for each temporary table and asserted 1..256 rows for every stage.
- Startup remains cancellation-aware and idempotent.
- SQLite, SQL Server LocalDB, and PostgreSQL migration lifecycles prove apply, backfill, downgrade, and reapply.
- The isolated migration is `20260817170000_BugTriage139ReceiptLookupIndexes` in each provider migration project; it follows the prior branch migration cleanly.

### Durable review narrative

- The exact tenth review was copied into the repository and hash-verified.
- The ninth temp file was absent; the receipt says so and does not fabricate an exact narrative.
- RED artifacts were committed before production edits.
- An intermediate post-production regression TRX is retained honestly under `tenth-intermediate`. It identified a seventh-review test fixture that used the current EF model against an old schema; the fixture now seeds target-schema SQL while keeping the exact downgrade-trigger assertions.

## GREEN validation

Repository-owned command/count receipt:
`docs/receipts/artifacts/BUG-TRIAGE-139/tenth-green/commands-and-counts.txt`

Focused tenth slice:
- Client: 18/18 passed, 0 failed, 0 skipped.
- Service/provider/artifact: 29/29 passed, 0 failed, 0 skipped.

Authoritative serialized UseCase scope:
- TRX `TestDefinitions`: 225.
- TRX `UnitTestResult`: 225.
- Counters: 225 total/executed/passed.
- 0 failed.
- 0 skipped/notExecuted.
- Discovery and execution are identical in the single authoritative invocation.

Contracts and adjacent scopes:
- Client UseCase: 30/30.
- REST/MCP: 17/17.
- Requirements wiki/transaction/renderer/DocFx: 74/74.
- Marker resolver/trust: 18/18.
- Every scope had 0 failures and 0 skips.

Serialized provider matrices, each started with zero competing `testhost`:
- SQLite UseCase: 22/22.
- SQL Server LocalDB UseCase: 16/16.
- PostgreSQL UseCase: 17/17.
- Every provider scope had 0 failures and 0 skips.

Build matrix:
- 12 affected production, migration, and test projects.
- Release, `--no-restore`, warnings as errors.
- 12 succeeded.
- 0 warnings.
- 0 errors.
- Exact project list is in `docs/receipts/artifacts/BUG-TRIAGE-139/tenth-green/build-matrix.txt`.

Repository gates:
- Correction (eleventh review): normal and `--ignore-space-at-eol` numstat were not materially identical for three files with mixed inserted terminators: `CreateUseCaseCommand.cs`, `CreateUseCaseFromFrCommand.cs`, and `UseCaseCqrsTests.cs`.
- The eleventh remediation normalized only those three files to their approved-base dominant LF style; byte-level audit proves no mixed terminators and the ignore-EOL semantic diff is empty for each.
- `git diff --check` passed before the GREEN commit.
- Final range/working-tree checks are recorded after the evidence commit.

## Exact slice files

RED tests:
- `tests/McpServer.Client.Tests/UseCaseClientTenthAdversarialReviewTests.cs`
- `tests/McpServer.Support.Mcp.Tests/Documentation/BugTriage139ReviewArtifactTests.cs`
- `tests/McpServer.Support.Mcp.Tests/Services/UseCaseTenthAdversarialReviewTests.cs`
- `tests/McpServer.Support.Mcp.Tests/Storage/UseCaseTenthProviderTests.cs`

Production/client/service/storage:
- `src/McpServer.Client/Models/UseCaseCreationTextValidator.cs`
- `src/McpServer.Client/Models/UseCaseModels.cs`
- `src/McpServer.Client/UseCaseClient.cs`
- `src/McpServer.Services/Models/UseCaseModels.cs`
- `src/McpServer.Services/UseCases/Commands/CreateUseCaseCommand.cs`
- `src/McpServer.Services/UseCases/Commands/CreateUseCaseFromFrCommand.cs`
- `src/McpServer.Services/UseCases/UseCaseCqrsHelpers.cs`
- `src/McpServer.Services/UseCases/UseCaseCreation.cs`
- `src/McpServer.Storage/Database/McpDatabaseMigrationCoordinator.cs`
- `src/McpServer.Storage/Entities/UseCaseEntity.cs`
- `src/McpServer.Storage/McpDbContext.cs`
- `src/McpServer.Storage/WorkspaceIdentity.cs`

Provider migrations:
- `src/McpServer.Storage.SqliteMigrations/Migrations/20260817170000_BugTriage139ReceiptLookupIndexes.cs`
- `src/McpServer.Storage.SqliteMigrations/Migrations/20260817170000_BugTriage139ReceiptLookupIndexes.Designer.cs`
- `src/McpServer.Storage.SqliteMigrations/Migrations/McpDbContextModelSnapshot.cs`
- `src/McpServer.Storage.SqlServerMigrations/Migrations/20260817170000_BugTriage139ReceiptLookupIndexes.cs`
- `src/McpServer.Storage.SqlServerMigrations/Migrations/20260817170000_BugTriage139ReceiptLookupIndexes.Designer.cs`
- `src/McpServer.Storage.SqlServerMigrations/Migrations/McpDbContextModelSnapshot.cs`
- `src/McpServer.Storage.PostgreSqlMigrations/Migrations/20260817170000_BugTriage139ReceiptLookupIndexes.cs`
- `src/McpServer.Storage.PostgreSqlMigrations/Migrations/20260817170000_BugTriage139ReceiptLookupIndexes.Designer.cs`
- `src/McpServer.Storage.PostgreSqlMigrations/Migrations/McpDbContextModelSnapshot.cs`

Test compatibility and evidence:
- `tests/McpServer.Support.Mcp.Tests/Storage/UseCaseSeventhProviderTests.cs`
- `docs/receipts/BUG-TRIAGE-139-ninth-remediation.md`
- `docs/receipts/BUG-TRIAGE-139-tenth-remediation.md`
- `docs/receipts/artifacts/BUG-TRIAGE-139/reviews/README.md`
- `docs/receipts/artifacts/BUG-TRIAGE-139/reviews/tenth-independent-review.txt`
- `docs/receipts/artifacts/BUG-TRIAGE-139/tenth-red/*`
- `docs/receipts/artifacts/BUG-TRIAGE-139/tenth-green/*`
- `docs/receipts/artifacts/BUG-TRIAGE-139/tenth-intermediate/usecase-before-downgrade-fixture-correction.trx`

## Supported MCP state

The supported TODO update succeeded and returned `success: true`; `BUG-TRIAGE-139` remains `done: false` with this remediation evidence appended.

Session-log persistence remained degraded: the supported plugin repeatedly timed out draining `client.SessionLog.SubmitAsync` after 30 seconds. Automatic failsafe state was retained. No cache was hand-edited.

## Remaining issue

Independent adversarial approval remains pending. There is no known product/test/build failure in this validated slice. The supported session-log drain timeout remains an operational persistence issue outside the remediation code; its automatic failsafe is retained.
