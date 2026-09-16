# Candidate Provider Validation Receipt

## Scope

- Candidate worktree: `C:\Users\kingd\AppData\Local\Temp\McpServer-validation\808ec049d56daf214c391beccd6b9d69bb9867c6-20260906T185957Z-319842a9`
- Commit: `808ec049d56daf214c391beccd6b9d69bb9867c6`
- Tree: `5091b64a8d9dade0bfb0b2b0295b93d464e60e1c`
- Configuration: `Release`
- SDK: `10.0.400`
- Provider evidence root: `C:\Users\kingd\AppData\Local\Temp\McpServer-validation-evidence\808ec049d56daf214c391beccd6b9d69bb9867c6-20260906T191653Z\provider-scope-20260906T194708Z`
- Provider runtime root: `<provider-evidence-root>\runtime-temp`, supplied through `MCP_TEST_TEMP_ROOT`
- Prior build and seven-project evidence: `docs/receipts/completion-program-20260906/candidate-windows-evidence-completion-20260906T194212Z.md`

The candidate retained the exact commit and tree and remained clean after provider validation: zero status entries and exit `0` from both unstaged and staged diff checks. No source, migration, database, service, WSL, deployment, or synchronization changes were made.

## Read-Only Prerequisites

- `SqlLocalDB.exe`: `C:\Program Files\Microsoft SQL Server\160\Tools\Binn\SqlLocalDB.exe`, file version `16.0.1000.6`.
- `SqlLocalDB.exe versions`: SQL Server 2022 `16.0.1000.6` and SQL Server 2025 `17.0.4025.3`; exit `0`.
- PostgreSQL binaries: `C:\Program Files\PostgreSQL\17\bin`; `postgres`, `initdb`, and `pg_ctl` all report `17.10`.
- Service state was observed only: `postgresql-x64-17` running; `MSSQLSERVER` stopped. No service was started or stopped.

The candidate fixture contract creates a GUID-named private LocalDB instance with the newest installed LocalDB engine, then stops and deletes only that instance during disposal. The PostgreSQL fixture can create a GUID-named ephemeral cluster beneath `MCP_TEST_TEMP_ROOT`, but no selected test at this candidate SHA uses it.

## Discovery

The requested filter was inspected against source and both relevant current Release test assemblies:

```text
FullyQualifiedName~ProviderDatabaseIntegrationTests|FullyQualifiedName~HandoffIngestionStorageMigrationTests
```

Actual discovery:

- `ProviderDatabaseIntegrationTests.Sqlite_CleanDatabase_AppliesMigrationsAndPersistsEntity`
- `ProviderDatabaseIntegrationTests.SqlServer_LocalDb_CleanDatabase_AppliesMigrationsAndPersistsEntity`

`git grep` against `HEAD` found no `HandoffIngestionStorageMigrationTests` or equivalent `Handoff*Storage*Migration` symbol. `McpServer.Support.Mcp.Tests.dll` reported no match for either requested class. `ProviderDatabaseIntegrationTests` contains exactly two `[Fact]` methods and has no PostgreSQL case.

Therefore the immutable candidate exposes two, not six, tests in the requested scope. The absent PostgreSQL provider case and three absent handoff storage-migration provider cases were not run, skipped, or counted as passing. This is a discovery gap in the exact candidate, not a test-run exclusion.

## Executed Provider Run

Command:

```powershell
dotnet test tests\McpServer.Support.Mcp.IntegrationTests\McpServer.Support.Mcp.IntegrationTests.csproj --configuration Release --no-build --no-restore --filter "FullyQualifiedName~ProviderDatabaseIntegrationTests" --logger "trx;LogFileName=ProviderDatabaseIntegrationTests.trx" --results-directory <provider-evidence-root>
```

No build was needed because the current exact-source Release solution binaries had already passed the full solution build recorded in the prior receipt and binlog.

TRX summary: outcome `Completed`, total `2`, executed `2`, passed `2`, failed `0`, error `0`, timeout `0`, aborted `0`, inconclusive `0`, notRunnable `0`, notExecuted `0`, disconnected `0`, warning `0`, inProgress `0`, pending `0`, result elements `2`, definition elements `2`.

Results:

- SQLite: `Passed`, duration `00:00:04.1216438`.
- Private SQL LocalDB: `Passed`, duration `00:00:23.1980854`.

Artifacts:

- `<provider-evidence-root>\ProviderDatabaseIntegrationTests.trx`
  - Bytes: `5350`
  - SHA-256: `D711A8C15FA9DEE18A90D28331A7C1EDBDAE73E30D422EBF83B1752AB35CB673`
- `<provider-evidence-root>\ProviderDatabaseIntegrationTests.console.log`
  - Bytes: `678`
  - SHA-256: `B34633483F70616AA64270E5ABFC4917B4E90E5A2F961C2E51EF95242AB04CC6`

## Conclusion

All provider tests discoverable in the requested classes at the immutable candidate SHA pass with machine-readable TRX evidence. The requested six-case provider scope is not complete because four expected tests do not exist in the candidate source or current Release assemblies; no result is fabricated for them.
