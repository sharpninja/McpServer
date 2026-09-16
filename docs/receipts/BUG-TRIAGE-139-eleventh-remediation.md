# BUG-TRIAGE-139 eleventh remediation receipt

Date: 2026-08-17
Branch: codex/bug-triage-139-remediation
Starting clean head: f61ed715598b6f3900cedc37036b2adde6308ebd
Base: 298c5fde3d1438ff7741ebec82ced796b207433e
Validated reconstructed GREEN code head: 9ad78596641de3fbe7d0a8962b1e90600bc0ceab
TODO state: open, done=false
Merge/push state: neither merged nor pushed

## Auditable chronology

- Original test-only RED commit: `6b70416916bfbdfc9385e51e41ed4794bd3863e2`.
- Original raw RED artifact commit while production remained unchanged: `aa06f3e94d59d1477de09a259cbbdeefdb4157c4`.
- Original GREEN production commit: `7ae2490507f4e7928c1b3a0b5d8a9deaf42456ff`.
- Backup ref created before reconstruction: `refs/backup/bug-triage-139-eleventh-pre-trailer-20260817` at `7ae2490507f4e7928c1b3a0b5d8a9deaf42456ff`.
- Earlier backup retained unchanged: `refs/backup/bug-triage-139-before-ninth-reconstruction-20260817`.
- Reconstruction used `refs/heads/codex/bug-triage-139-eleventh-reconstruction`; the feature branch moved only after all equivalence checks passed.
- Reconstructed RED commits: `55f1571af4a632635bc518e1ccf39e27cfa432bc` and `1abe544cc3fc4a20373817011b20c134fb2991a8`.
- Reconstructed GREEN code commit: `9ad78596641de3fbe7d0a8962b1e90600bc0ceab`.
- Complete 24-entry old-to-new map: `docs/receipts/artifacts/BUG-TRIAGE-139/eleventh-green/history-reconstruction-map.txt`.
- Every old/new pair has the same tree, author identity/date, committer identity/date, order, and non-AI-trailer message content. The final tree stayed `9f7b1cb088eeaaf35527141be12fb12f8fa9adfd`.
- Git trailer parsing recognizes exactly one adjacent `AI-Signature` and `AI-Confidence` pair on every feature commit.

The evidence commit containing this receipt is necessarily identified by Git after the receipt content is formed; the exact final branch head is reported by the final `git rev-parse HEAD` evidence and handoff response.

## Exact independent review provenance

- Repository-owned narrative: `docs/receipts/artifacts/BUG-TRIAGE-139/reviews/eleventh-independent-review.txt`.
- Source: exact copy of `C:\Users\kingd\AppData\Local\Temp\PowerShell.MCP.Output\pwsh_output_20260817_132000_948_ldn3kzmq.qdu.txt`.
- SHA-256: `E7BDCDC7649693318699A95403688882F9E2DA33ED0ED0D9B0DCC59B217454DF`.
- The audit test verifies the repository artifact hash; no Temp dependency is required afterward.

## RED evidence

Raw RED console and TRX artifacts are under `docs/receipts/artifacts/BUG-TRIAGE-139/eleventh-red/`. The authoritative summary is `red-summary.txt`.

Marker trust command:
- 7 executed, 0 passed, 7 failed, 0 skipped.
- `ResolveTrustedMarkerAsyncCore_HealthyNonce_ComparesCacheOnlyAfterTrust`
- `ResolveTrustedMarkerAsyncCore_HealthTimeout_PerformsZeroPhysicalComparisons`
- `ResolveTrustedMarkerAsyncCore_HealthFailure_PerformsZeroPhysicalComparisons`
- `TryResolveWithDiagnostics_ValidUncWorkspace_ReturnsTrustedWorkspaceAfterNonce`
- `ResolveTrustedMarkerAsyncCore_HealthCancellation_RethrowsBeforePhysicalComparison`
- `ResolveTrustedMarkerAsyncCore_SignedWrongNonce_PerformsZeroPhysicalComparisons`
- `ResolveTrustedMarkerAsyncCore_OversizedMarker_RejectsBeforeHealthOrPhysicalAccess`

Consumer/storage/audit command:
- 11 executed, 2 passed, 9 failed, 0 skipped.
- `PreBugTriage139SqlServerMigrations_AreByteIdenticalToBase`
- `EleventhReviewNarrative_IsRepositoryOwnedAndMatchesProvenanceHash`
- `FederationCollisionPreflight_MultipleCollisions_ReportsDeterministicFirstPair`
- `FederationCollisionPreflight_CancelledAfterStageCreation_LeavesNoTemporaryTables`
- `FeatureHistory_ContainsExactlyOneFormalAdjacentAiTrailerPairPerCommit`
- `IdentityPreflightCleanup_FirstDropFails_CleansIndependentlyAndSecondCallSucceeds`
- `FederationCollisionPreflight_OverBatchSize_StagesBoundedProviderBatches`
- `ChangedCodeAndTestTextFiles_UseOneDominantByteLevelTerminatorStyle`
- `FederationCollisionPreflight_SourceUsesBoundedStagingAndOrderedLimitedDiagnostics`

Provider-native federation cancellation:
- SQLite: 1 executed, 0 passed, 1 failed, 0 skipped; `EnsureFederatedWorkspaceRow_BlockedRead_CancelsNatively`.
- SQL Server LocalDB: 2 executed, 1 passed, 1 failed, 0 skipped; `EnsureFederatedWorkspaceRow_BlockedRead_CancelsNatively`.
- PostgreSQL: 2 executed, 1 passed, 1 failed, 0 skipped; `EnsureFederatedWorkspaceRow_BlockedRead_CancelsNatively`.

Aggregate RED: 23 executed, 4 passed, 19 failed, 0 skipped.

Evidence qualification: the initial deterministic-collision provider fixture used duplicate raw paths, so that one named RED failed at the pre-existing raw unique constraint before reaching the intended canonical-collision assertion. It is not claimed as stand-alone causal proof. The companion source-contract RED directly proved the lifetime dictionary and unordered diagnostic implementation. The corrected provider fixture now reaches the semantic collision path and passes.

## Remediation

### Nonce trust before marker-controlled physical access

Both resolver entry points now share one bounded asynchronous trust-first core:

1. Locate and read a marker no larger than 256 KiB.
2. Parse required fields.
3. Verify the HMAC.
4. Verify a fresh nonce against the live health endpoint with bounded timeout and cancellation.
5. Only after those trust steps, consult/replace the cache or invoke physical workspace comparison.

Wrong/stale nonce, timeout, health failure, malformed response, and cancellation cannot trigger physical/UNC/cache comparison. Valid trusted UNC workspaces and cache reuse remain supported.

### Bounded federation collision preflight

Federation backfill no longer retains a lifetime dictionary. It reads 256-row keyset batches, stages bounded rows in provider temporary tables, and performs provider-side collision detection. SQLite has real >batch-size coverage; SQL Server and PostgreSQL provider tests cover migration behavior.

### Deterministic diagnostics and cleanup

Workspace, receipt, and federation collision diagnostics are explicitly ordered and limited. Temporary tables are dropped independently. Cleanup failures do not hide the primary exception: failures are aggregated deterministically, and a failed first drop does not prevent cleanup of later resources. Cancellation/failure tests prove no lingering tables on caller-owned connections and a second invocation succeeds.

### Provider-native federation cancellation

Federation workspace candidate probes and connection opening are asynchronous and cancellation-token aware. SQLite uses a bounded native busy timeout and interrupt-aware operation path; SQL Server and PostgreSQL use async provider calls. The tests prove native lock waits before cancellation through SQLite locking, SQL Server DMV state, and PostgreSQL blocking/activity state.

### Historical migration immutability

These seven pre-BUG-TRIAGE-139 SQL Server migration files are byte-for-byte restored from base:

- `20260701205117_Decompose4nfSessionLogCommitFiles.cs`
- `20260701210343_Decompose4nfTriageReportLists.cs`
- `20260701211220_Decompose4nfWorkspaceBannedItems.cs`
- `20260701220645_Decompose4nfRequirementAcceptanceCriteria.cs`
- `20260702014138_Decompose4nfTodoItemLists.cs`
- `20260702192410_Decompose4nfTodoDocumentMetadata.cs`
- `20260702193940_Decompose4nfAgentModelLists.cs`

No new schema repair was necessary for this finding; current forward identity migrations remain isolated at the branch tip. The audit test compares each historical Git blob to base. Fresh/current and pre-hardening baseline upgrade/downgrade provider paths remain covered by the serialized provider matrices.

### Platform test contract

`WorkspaceServiceAndUseCaseCreation_CaseVariantReuseOneStoredIdentity` now has a substantive POSIX case-sensitive assertion instead of a non-Windows return. The branch audit scans this test for the prohibited guard/return shape; all changed test scopes execute with zero skips.

### Formal audit trailers

The rewrite was message-only and non-destructive. A backup ref preceded reconstruction, a separate reconstruction ref held the candidate chain, and the feature ref moved atomically only after tree, metadata, order, body, and formal trailer checks passed.

### Line endings

The tenth receipt's inaccurate numstat statement is corrected in place. The exact mixed-terminator scope was:

- `src/McpServer.Services/UseCases/Commands/CreateUseCaseCommand.cs`
- `src/McpServer.Services/UseCases/Commands/CreateUseCaseFromFrCommand.cs`
- `tests/McpServer.Support.Mcp.Tests/Services/UseCaseCqrsTests.cs`

Only those inserted terminators were normalized to each file's approved-base dominant LF style. The byte-level audit passes and the ignore-EOL semantic diff for these three files is empty.

The expanded audit enumerates all editable repository text extensions and compares pre-existing files to the approved base at byte level. Raw `.trx`, `*console.txt`, and exact independent-review copies are deliberately not normalized because they are immutable source evidence; their original bytes are covered by the SHA-256 inventory. The first overly broad audit failure is retained as `eleventh-intermediate/all-text-eol-audit-expanded-red.trx`; the policy-correct editable-text audit passes in `all-editable-text-eol-audit-pre-evidence.trx`.


## GREEN evidence

Focused eleventh replay:
- Marker trust: 7/7.
- Consumer/storage/audit: 11/11.
- Federation providers: 5/5.
- Aggregate: 23/23.
- Failures: 0.
- Skips: 0.

Authoritative serialized UseCase scope:
- TRX `TestDefinitions`: 226.
- TRX `UnitTestResult`: 226.
- Total/executed/passed: 226/226/226.
- Failed: 0.
- Skipped/notExecuted: 0.
- One authoritative invocation therefore discovered and executed the identical complete scope.

Contracts and adjacent scopes:
- Client UseCase: 30/30.
- REST/MCP: 17/17.
- Requirements wiki/transaction/renderer/DocFx: 74/74.
- Marker resolver/cache/trust: 24/24.
- All had zero failures and zero skips.

Serialized providers, each started with zero competing `testhost`:
- SQLite UseCase: 22/22.
- SQL Server LocalDB UseCase: 16/16.
- PostgreSQL UseCase: 17/17.
- Federation provider matrix: 5/5.
- Workspace/federation preflight focus: 5/5.
- All had zero failures and zero skips.

The first full UseCase attempt had two 120-second LocalDB command timeouts (224/226). Each exact test immediately passed alone (1/1 and 1/1), and the subsequent complete authoritative run passed 226/226 in 2 minutes 56 seconds. Raw failed, isolated, and green TRXs are retained; this is classified as a transient LocalDB stall, not a reproducible product failure.

Builds:
- 12 affected production, migration, and test projects.
- Release, `--no-restore`, warnings as errors.
- 12 succeeded.
- 0 warnings.
- 0 errors.

Repository gates:
- Historical migration blob audit: passed.
- Formal trailer audit: passed.
- Byte-level EOL audit: passed.
- `git diff --check`: passed before the GREEN commit; rerun after evidence commit.
- Full commands, counts, artifacts, and build list: `docs/receipts/artifacts/BUG-TRIAGE-139/eleventh-green/commands-and-counts.txt`.

## Exact eleventh slice files before this receipt

Evidence:
- `docs/receipts/artifacts/BUG-TRIAGE-139/eleventh-red/*`
- `docs/receipts/artifacts/BUG-TRIAGE-139/eleventh-green/*`
- `docs/receipts/artifacts/BUG-TRIAGE-139/reviews/eleventh-independent-review.txt`
- `docs/receipts/BUG-TRIAGE-139-tenth-remediation.md`
- this receipt

Production:
- `src/McpServer.Repl.Host/MarkerFileClientOptionsResolver.cs`
- `src/McpServer.Services/Services/FederationTopologyService.cs`
- `src/McpServer.Services/UseCases/Commands/CreateUseCaseCommand.cs`
- `src/McpServer.Services/UseCases/Commands/CreateUseCaseFromFrCommand.cs`
- `src/McpServer.Storage/Database/McpDatabaseMigrationCoordinator.cs`
- the seven SQL Server migration files listed above

Tests:
- `tests/McpServer.Repl.IntegrationTests/MarkerFileNonceTrustEleventhTests.cs`
- `tests/McpServer.Repl.IntegrationTests/MarkerFileTrustNinthTests.cs`
- `tests/McpServer.Support.Mcp.Tests/Documentation/BugTriage139EleventhRepositoryAuditTests.cs`
- `tests/McpServer.Support.Mcp.Tests/Services/UseCaseAdversarialReviewTests.cs`
- `tests/McpServer.Support.Mcp.Tests/Services/UseCaseCqrsTests.cs`
- `tests/McpServer.Support.Mcp.Tests/Storage/FederationEleventhProviderTests.cs`
- `tests/McpServer.Support.Mcp.Tests/Storage/UseCaseIdentityMigrationProviderTests.cs`
- `tests/McpServer.Support.Mcp.Tests/Storage/WorkspaceIdentityEleventhProviderTests.cs`

## Open state

- BUG-TRIAGE-139 remains open with `done=false`.
- No merge or push occurred.
- No known product or validation issue remains in this slice.
- The documented transient LocalDB stall did not reproduce in isolated tests, the provider matrix, or the complete authoritative rerun.
- Independent approval remains pending.
