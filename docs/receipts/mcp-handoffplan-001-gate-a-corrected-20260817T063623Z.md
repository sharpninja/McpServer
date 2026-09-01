# GATE A corrected: mutation-backed RED then GREEN

Written: 2026-08-17T06:36:23Z
Session: GrokCode-20260816T175800Z-mcp-handoff-001
Turn: req-20260817T055051Z-012-gate-a-only-correct-all-items
Isolated console: PowerShell.Mcp Window #45336 Edam (earlier lock window #50432 was discarded after Add-Type held McpServer.Services.dll)

## Honesty about the prior receipt

`docs/receipts/mcp-handoffplan-001-gate-a-mutation-red-20260817T041322Z.md` is the incomplete source of truth this turn corrects. That receipt said Gate A was unfinished. It recorded:

- Fingerprint title RED, then a disputed GREEN (stale mutated DLL under an isolated theory filter).
- HTTP RunNotFound RED 400, then residual 400 after restore (source/runtime mismatch).
- Lease fencing that was brittle: fence-only mutation did not fail after the honesty fix; `CreatedTodoId` asserts were flaky.
- Approval, compensation, enum/workspace, bounded/template/provider, and dead-contract groups without isolated RED/GREEN pairs.
- `EfTodoService.CreateAsync_SameKeyChangedPayload_Conflicts` claimed red on current `AreEquivalent` code.

This receipt does not relabel that earlier work as complete. It re-ran every required Gate A item with a rebuild before each restored GREEN.

## Method

Production files were copied to `C:\Users\kingd\AppData\Local\Temp\handoff-gate-a-20260817T055051Z` (outside the repo). Each cycle mutated one production file (or one dead-contract pair), ran the targeted test, restored from that backup, rebuilt, compared SHA-256 to the intended fixed baseline, then reran GREEN. No `.bak` / mutation artifact remains in the repo. After an accidental `Add-Type` of `McpServer.Services.dll` locked the default bin, later mutation cycles used unique `%TEMP%\handoff-gate-a-bin-*` output folders; the final focused suites ran against the project output folders after that lock was released.

Rebuild-before-GREEN is required. The prior incomplete receipt's HTTP 400-after-restore and SameKeyChangedPayload "still red" claims were stale-DLL false results. After `--no-incremental` / clean rebuild they were green on the same source.

## Product change made so the lease fence is observable

`HandoffIngestionService.IngestAsync`: when `OwnsProcessingAsync` is false, the stale owner still calls `CompleteReservedRunAsync` with `created: false`. The owner+`StateVersion` predicate must no-op. Removing that predicate clobbers the takeover `Created` receipt (`ReviewState` becomes `None`). Baseline after this change: `76BB71C916E67F51ECAFD0B5FE71527C4B83D88EA927586BEAAFFFB0D0106D29`.

`HandoffDurabilityTests` lease/approval cases now poll for owner assignment instead of `Task.Delay`, and they assert the one-TODO plus durable `Created` receipt invariants.

`HandoffDeadContractInventoryTests` added (reflection, entity schema, source/migration scan).

## Cycles

### 1. Fingerprint title

- File: `src/McpServer.Services/Services/TodoPayloadFingerprint.cs`
- Test: `TodoPayloadFingerprintTests.AreEquivalent_AnySemanticMismatch_IsFalse` (title case)
- Before: `660EFB968A44A836CB17C49791DDE4592ACFE2DC08E2FE39F811530D7DD94B5A`
- Mutation: `Append(builder, "title", string.Empty)`
- Mutated: `E16B23160F0C8170E3C956DC4E82AAA84D1F07BDEB29AC2DFDD635052E0383BF`
- RED: Failed 1 / Passed 9 / Skipped 0. Exit 1. Title case Expected False Actual True.
- Restore hash equal to before.
- GREEN after rebuild: Failed 0 / Passed 10 / Skipped 0. Exit 0.

### 1b. EfTodo SameKeyChangedPayload

- File: `src/McpServer.Services/Services/EfTodoService.cs`
- Tests: `EfTodoServiceTests.CreateAsync_SameKeyExactPayload_Heals`, `EfTodoServiceTests.CreateAsync_SameKeyChangedPayload_Conflicts`
- Current-code after rebuild, before mutation: Failed 0 / Passed 2. The prior receipt's red on current `AreEquivalent` code was a stale DLL. No product fix was required.
- Mutation: heal on idempotency key only (drop `TodoPayloadFingerprint.AreEquivalent`)
- Mutated: `45E19E21A067BE698F49A7F7E6EBF02D322BC6C9348DDFA4E1B483CC5F419396`
- RED: Failed 1 / Passed 0. `CreateAsync_SameKeyChangedPayload_Conflicts` Expected False Actual True.
- Restore: `EFB7C0630D47F9D0A7E25DA44843EEDF90D5F2A0F6AE646353A19273D693761D` (baseline match).
- GREEN after rebuild: Failed 0 / Passed 2 / Skipped 0. Exit 0.

### 2. HTTP RunNotFound

- File: `src/McpServer.Services/Services/HandoffHttpStatus.cs`
- Test: `HandoffStrictEnumAndHttpTests.FromErrorCode_MapsStableStatuses` (`code: "run_not_found"`, `status: 404`)
- Before: `CB44FE5F5841539EE9AE644B4FCF8A2BFD2746D889D1CC36D736A607476E7EEB`
- Mutation: `RunNotFound => 400`
- Mutated: `2152A11C2BBF110C9C19373836344D038273869F93BBCE035D5824D4E5A59F42`
- RED: Failed 1 / Passed 7 / Skipped 0. Expected 404 Actual 400. Exit 1.
- Restore hash equal to before. Source line remains `HandoffErrorCodes.RunNotFound => 404`.
- GREEN after rebuild: Failed 0 / Passed 8 / Skipped 0. Exit 0.
- Source/runtime mismatch from the incomplete receipt is resolved by rebuild-before-GREEN.

### 3. Lease fencing

- File: `src/McpServer.Services/Services/HandoffIngestionService.cs`
- Test: `HandoffDurabilityTests.IngestAsync_LeaseExpiresDuringLiveExtraction_TakeoverWinsAndFirstCannotCreate`
- Fixed baseline: `76BB71C916E67F51ECAFD0B5FE71527C4B83D88EA927586BEAAFFFB0D0106D29`
- Mutation: `CompleteReservedRunAsync` `Where` reduced to `run.RunId == entity.RunId` (owner+version fence removed)
- Mutated: `B072B5DE6F32A91F6ED049B69A94B41E77416A6607ED521EA62E9691C4D27442`
- RED: Failed 1 / Passed 0. Stored `ReviewState` Expected `"Created"` Actual `"None"`. Exit 1.
- Restore hash equal to fixed baseline.
- GREEN after rebuild: Failed 0 / Passed 1 / Skipped 0. Exit 0.
- Invariants kept: `takeover.Created`, `!first.Created`, `CreatedCount==1`, durable `CreatedTodoId` / `ReviewState=Created`.

### 4. Approval fencing

- File: same `HandoffIngestionService.cs`
- Tests: `HandoffDurabilityTests.ApproveAsync_LeaseExpiresDuringLiveCreate_SecondInstanceWins`, `HandoffDurabilityTests.ApproveAsync_LiveClaimant_RejectsStaleSecondClaim`
- Mutation: `TryCompleteApprovalAsync` `Where` reduced to `run.RunId == entity.RunId`
- Mutated: `9C0054C897C795A2E539DC77C9AEAD61F7076BC82D3FE72BF662B8B864AF892E`
- RED: Failed 1 / Passed 1. Stale-takeover test: `first.Created` Expected False Actual True. Live-claimant still passed.
- Restore hash equal to `76BB71C916E67F51ECAFD0B5FE71527C4B83D88EA927586BEAAFFFB0D0106D29`.
- GREEN after rebuild: Failed 0 / Passed 3 (lease + both approval tests) / Skipped 0. Exit 0.

### 5. Compensation durability

- File: same `HandoffIngestionService.cs`
- Test: `HandoffDurabilityTests.SaveRunAfterTodo_TodoAbsent_DoesNotReportCreatedFromMemory`
- Mutation: ignore missing TODO (`_ = todo`) and still write Created
- Mutated: `9149D0EC7CB290C179C3D752B984C70F7C9121AD1651071B952965732A452E03`
- RED: Failed 1 / Passed 0. `result.Created` Expected False Actual True. Exit 1.
- Restore hash equal to fixed baseline.
- GREEN after rebuild: Failed 0 / Passed 1 / Skipped 0. Exit 0.

### 6. Strict enum (service, REST converter, MCP, REPL)

- Service file: `HandoffIngestionService.cs`. Test: `HandoffDurabilityTests.IngestAsync_UndefinedMode_DoesNotCreate`. Mutation: drop `Enum.IsDefined` gate. Mutated `91CCF46B9523E3961B45505377B8578D1AE45611715E97F5120AC5067FE13479`. RED Failed 1 (Created Expected False Actual True). Restore baseline match. GREEN Failed 0 / Passed 1.
- Converter file: `HandoffStrictStringEnumConverter.cs` baseline `E83A6D8040B02C3C45D803DD909E0059D86E65D70FF80980CFB2B9EB91D9E1A9`. Test: `HandoffStrictEnumAndHttpTests.StrictEnumConverter_Numeric999_Throws`. Mutation: accept integer tokens. RED Failed 1.
- MCP file: `FwhMcpTools.Handoff.cs` baseline `44778A27462A0308EE9AF391CDA7A803B62A8741B34E7657FFD46520D75E32AE`. Test: `HandoffMcpToolTests.HandoffIngest_NumericMode999_ReturnsInvalidMode`. Mutation: drop `Enum.IsDefined` on mode. RED Failed 1.
- REPL file: `ReplCommandDispatcher.cs` baseline `61C6FA4F4BBCA6B99A5F7B51EB60E75E4F85F5090737D9785283E90CACC911D3`. Test: `HandoffWorkflowTests.Dispatcher_NumericMode999_ReturnsInvocationError`. Mutation: drop defined-name check. RED Failed 1.
- Combined converter+MCP RED: Failed 2 / Passed 0. REPL RED: Failed 1 / Passed 0.
- Restore hashes match all three baselines.
- GREEN after rebuild: Support Failed 0 / Passed 2; Repl.Core Failed 0 / Passed 1.

### 7. Canonical workspace, bounded Path, bounded Artifact

- `HandoffWorkspacePaths.cs` baseline `2C59C7141F2994D9D4460DDDC61A289B59C3ABA39EB2FF7F60F26F0049B0649F`. Mutation: `canonical = workspacePath` (no `GetFullPath`). Test: `HandoffWorkspacePathsTests.Canonicalize_RelativeAndNested_MatchGetFullPath`. RED Failed.
- `HandoffContainedFileReader.cs` baseline `2CAA208165EB90B2FC022E6B79C5395ED5A305CD03C8D7380128EC8688656BAA`. Mutation: remove 8 MiB stop. Tests: `HandoffContainedFileReaderTests.ReadBoundedAsync_GrowingStream_StopsAtLimit`, `HandoffBoundedSourceTests.ResolveAsync_OversizedPath_FailsClosed`. RED Failed.
- `HandoffSourceResolver.cs` baseline `FCD8E59E4B8880615E2213003FAEBC200AA9CE00C2E95CA2622A62E3EFA5E6A5`. Mutation: skip artifact decoded-byte check. Test: `HandoffBoundedSourceTests.ResolveAsync_OversizedArtifactChunks_FailsBeforeJoin`. RED Failed.
- Combined RED: Failed 4 / Passed 0 / Skipped 0.
- Restore hashes match. GREEN after rebuild: Failed 0 / Passed 4 / Skipped 0.

### 8. Custom template, provenance redaction, provider-number classification

- Template: `HandoffIngestionService.cs`. Test: `HandoffDurabilityTests.IngestAsync_CustomPromptTemplate_IsRejected`. Mutation: accept custom `PromptTemplateId`. RED Failed 1 (`Success` Expected False Actual True). Restore baseline. GREEN Failed 0 / Passed 1.
- Provenance: same file. Test: `HandoffDurabilityTests.IngestAndApprove_CredentialBearingProvenance_IsSanitized`. Mutation: `SanitizeText` returns raw value. RED Failed 1 (found `supersecretvalue`). Restore baseline. GREEN Failed 0 / Passed 1.
- Provider: `HandoffDbExceptions.cs` baseline `45227DFD861C8CD323D1A6EA5D2A0E360E73FD69F2C9600F9B5FD76F0463C9D0`. Test: `HandoffDbExceptionsTests.IsCommitAmbiguous_EnglishMessageOnly_IsFalse`. Mutation: treat English `"timeout"` as ambiguous. RED Failed 1 Expected False Actual True. Restore baseline. GREEN Failed 0 / Passed 1.

### 9. ReplayOfRunId and Approved dead-contract inventory

- New tests: `HandoffDeadContractInventoryTests.HandoffReviewState_DoesNotDefineApproved`, `HandoffDeadContractInventoryTests.HandoffIngestionRunEntity_DoesNotExposeReplayOfRunId`, `HandoffDeadContractInventoryTests.SourceAndSchema_DoNotReintroduceReplayOfRunIdOrApproved`
- Restored GREEN first: Failed 0 / Passed 3.
- Mutation: add `HandoffReviewState.Approved = 2` and `HandoffIngestionRunEntity.ReplayOfRunId`.
- RED: Failed 3 / Passed 0 / Skipped 0. Exit 1.
- Restore: models `C2BC5D2B2F0DA9300DA215BC4EFBD78C5962C12073F0B23F4887230BE423F22F`, entity `8DF52D6CA354E9736CFDD6E422BDA8A1A1EB74E70A22CAEEB917A30DB5E7A52E`.
- GREEN after rebuild: Failed 0 / Passed 3 / Skipped 0.

### 10. UNC and prompt-redaction restored GREEN after rebuild

- Tests: `HandoffContainedFileReaderTests.NormalizeFinalPath_UncDevicePrefix_BecomesUncPath`, `OneShotSensitivePromptPolicyTests.Publish_HandoffContext_RedactsRawSource`
- Combined with provider GREEN after rebuild: Failed 0 / Passed 3 / Skipped 0 (those two plus English-message provider test).
- File hashes unchanged from intended baselines (`HandoffContainedFileReader` `2CAA2081...`, `OneShotSensitivePromptPolicy` `28B403DD...`).

## Final production hash check

All intended fixed baselines matched after the last restore. No `MUTATION:` comments remain. No repo `.bak` / mutation artifacts.

## Focused unit scope (no integration, no provider/migration)

Run against project output folders after the Services.dll lock was released. Filter on Support: `Category!=Integration` (that project has no Integration-category tests).

- `tests/McpServer.Client.Tests`: Failed 0 / Passed 281 / Skipped 0. Exit 0. Log: `%TEMP%\handoff-gate-a-20260817T055051Z-focused-client.log`
- `tests/McpServer.Repl.Core.Tests`: Failed 0 / Passed 826 / Skipped 0. Exit 0. Log: `%TEMP%\handoff-gate-a-20260817T055051Z-focused-repl.log`
- `tests/McpServer.Support.Mcp.Tests` `Category!=Integration`: Failed 0 / Passed 1955 / Skipped 0. Exit 0. Log: `%TEMP%\handoff-gate-a-20260817T055051Z-focused-support.log`

A first Repl/Support pass using `-o %TEMP%\handoff-gate-a-bin-*` produced false `FindRepoRoot` failures (`McpServer.sln` not an ancestor of the output folder). Those counts are not used.

## Unresolved Gate A items

Zero.

## Explicitly deferred

Gate B (full Support.Mcp.IntegrationTests, Marker/QuadBrain suite-load) is not run in this invocation.

SQLite / SQL Server / PostgreSQL migration and provider tests are not run. BUG-TRIAGE-139 is using provider resources.

MCP-HANDOFF-001, MCP-HANDOFFPLAN-001, and MCP-HANDOFFREVIEW-001 remain `Done=false`. No commit, merge, or push.

Unrelated dirty work (SessionLogSanitizer timeout changes, compressed historical FR/TR acceptance criteria) was not used as handoff evidence and was not modified for this receipt.
