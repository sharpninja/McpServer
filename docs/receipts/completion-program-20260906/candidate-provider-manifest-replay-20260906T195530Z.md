# Candidate Provider Manifest Replay Receipt

## Scope

- Candidate: `C:\Users\kingd\AppData\Local\Temp\McpServer-validation\808ec049d56daf214c391beccd6b9d69bb9867c6-20260906T185957Z-319842a9`
- Commit: `808ec049d56daf214c391beccd6b9d69bb9867c6`
- Tree: `5091b64a8d9dade0bfb0b2b0295b93d464e60e1c`
- Test project: `tests\McpServer.Support.Mcp.Tests\McpServer.Support.Mcp.Tests.csproj`
- Configuration: `Release`
- Replay evidence root: `C:\Users\kingd\AppData\Local\Temp\McpServer-validation-evidence\808ec049d56daf214c391beccd6b9d69bb9867c6-20260906T191653Z\provider-manifest-replay-20260906T195229Z`
- Historical scope manifest: `C:\Users\kingd\AppData\Local\Temp\BUG-TRIAGE-139-twenty-second-20260905T231500Z\final\commit-78cdb9f9-exact-final\provider-manifest\twenty-second-final-provider-manifest.trx`

No source, history, service, WSL, migration, deployment, synchronization, or host configuration was changed. The candidate was clean at the required SHA/tree before and after the gate.

## Historical Manifest Verification

The source manifest SHA-256 is `E00969C662678107F45E2C0EC2683A7D284BE1055D895952F89674E4BCD168C3`, exactly matching the required hash.

Parsed historical counters and identities:

- Outcome: `Completed`
- Total/executed/passed/failed/notExecuted: `107/107/107/0/0`
- Result elements and unique result IDs: `107/107`
- Test definitions and unique definition IDs: `107/107`
- Result IDs without definitions: `0`
- Unique `TestMethod.className + name` identities: `107`
- Unique classes: `27`

The manifest includes service provider tests plus SQLite, SQL Server, and PostgreSQL storage-provider families. It specifically includes `PostgreSqlDurableIdentifierTwelfthProviderTests` and the PostgreSQL UseCase, Federation, and WorkspaceIdentity families, with corresponding SQLite and SQL Server classes.

The historical `107` passes define scope only. They are not counted as current-candidate results.

## Current Discovery Mapping

A frozen filter was generated exclusively from the 107 manifest `TestMethod.className + name` identities. It contains 107 exact `FullyQualifiedName=` clauses and is 15,711 characters long.

Current Release discovery result:

- Discovery exit: `0`
- Historical identities: `107`
- Current unique discovered identities: `107`
- Missing from current discovery: `0`
- Additional current discovery: `0`
- Discovery duration: `5.758s`

Machine-readable mapping:

- `manifest-to-current-discovery.csv`: 47,007 bytes; SHA-256 `F69C4466EA63D6E6280854A5F6CB7D320A01998E0927586DAC25C1A9831E970F`
- `current-discovery.log`: 14,500 bytes; SHA-256 `5B46A5F39FB1C2F943411594DDB199E7D685AA2CB5BF7A7FAF6B1944AC24B887`
- `frozen-filter.csv`: 15,770 bytes; SHA-256 `A21CD6E0A612262F0623A1D0BC93794105DB36E9AAF7304D01B4298D148CBD43`

## Current Frozen Replay

Command shape, with the exact frozen filter stored above:

```powershell
dotnet test tests\McpServer.Support.Mcp.Tests\McpServer.Support.Mcp.Tests.csproj --configuration Release --no-build --no-restore --filter <107 exact manifest identities> --logger "trx;LogFileName=current-provider-manifest-replay.trx" --results-directory <replay-evidence-root>
```

No build or binlog was needed because the current exact-source Release binary was already produced by the successful full-solution build recorded in `candidate-windows-evidence-completion-20260906T194212Z.md`.

Current run result after `160.293s`:

- Process exit: `1`
- TRX outcome: `Failed`
- Total/executed/passed/failed: `105/105/87/18`
- Error/timeout/aborted/inconclusive/notRunnable/notExecuted/skipped-equivalent counters: all `0`
- Result elements and unique result IDs: `105/105`
- Test definitions and unique result FQNs: `105/105`
- Result identities not present in discovery: `0`

Two tests were discovered but produced no TRX result. They are not labeled NotRun because the TRX `notExecuted` counter is `0`:

- `McpServer.Support.Mcp.Tests.Services.TunnelProviderTests.CloudflareProvider_StartAsync_WhenCliMissing_SetsError`
- `McpServer.Support.Mcp.Tests.Services.TunnelProviderTests.FrpProvider_ProviderName_IsFrp`

All 18 failures are PostgreSQL fixture-constructor failures with the same message: `EphemeralPostgresFixture` threw because `initdb.exe` exited `1`.

Failure counts by class:

- `PostgreSqlDurableIdentifierTwelfthProviderTests`: `1`
- `PostgreSqlFederationEleventhProviderTests`: `2`
- `PostgreSqlUseCaseFifthProviderTests`: `6`
- `PostgreSqlUseCaseSeventhProviderTests`: `2`
- `PostgreSqlUseCaseSixthProviderTests`: `3`
- `PostgreSqlUseCaseTenthProviderTests`: `2`
- `PostgreSqlWorkspaceIdentityNinthProviderTests`: `2`

Every current SQLite and SQL Server result in the manifest scope passed. The service-provider families produced 36 passes and two discovered-without-result identities. The seven PostgreSQL classes produced 18 failures and no passes.

Current machine-readable result evidence:

- `current-provider-manifest-replay.trx`: 233,053 bytes; SHA-256 `C4B057A7FE59C70CC4B7388F071EC92CF49D7FD5214AB744EA22F510F32D21F1`
- `current-provider-manifest-replay.console.log`: 24,891 bytes; SHA-256 `EE1341B23F5F44FA9C7C92BDCE468691B04D1D563258B65A20A80CE3E26D61EA`
- `manifest-to-current-results.csv`: 41,462 bytes; SHA-256 `6E1FF04AE732AE82C5D3C4A2AEB57BEB508ABEAB83AE7FD961F69C3BFB426F16`
- `current-results-by-class.csv`: 2,816 bytes; SHA-256 `E56C86FAE5033AE61CEE449B85056AF4653F26BF6250CE67A2F384A3CA4E7418`

## PostgreSQL Diagnostic Boundary

The fixture's `RunTool` does not redirect or retain native stdout/stderr, so the TRX preserves only `initdb.exe failed with exit code 1`; the underlying native message is unavailable. The seven selected PostgreSQL test classes have separate `IClassFixture<EphemeralPostgresFixture>` declarations, no assembly-level test-parallelization override was found, and their fixture failures occurred within a 3.10-second window. This is consistent with concurrent fixture startup but does not prove a specific native cause.

A bounded diagnostic invoked PostgreSQL 17.10 `initdb` once with the same binary, same replay `MCP_TEST_TEMP_ROOT`, same authentication/encoding options, and a 221-character data-directory path. It succeeded with exit `0`. This rules out a generally missing/broken PostgreSQL installation and rules out that tested path length as the immediate cause; it does not convert the failed test results into passes.

- `initdb-diagnostic.log`: 1,326 bytes; SHA-256 `F976E50E4519A2D6CAC51D799D94F5A580A4991C2E928C965AABF276F4069BE7`

## Cleanup

Only the diagnostic cluster and password file beneath the verified replay runtime-temp root were removed. Final cleanup checks reported:

- Runtime-temp entries: `0`
- Private `mcp-*` LocalDB instances: `0`
- Processes referencing the replay or runtime-temp paths: `0`
- Candidate status entries: `0`
- Unstaged/staged diff exit codes: `0/0`

## Gate Status

The G1 manifest-to-current discovery mapping is exact: 107 historical identities map to 107 current discoveries with no missing or additional identities. The current provider execution gate is failed, not green: 87 passed, 18 failed, and two discovered tests have no result. The historical 107-pass manifest is retained solely as the frozen scope definition.
