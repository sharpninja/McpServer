# Candidate Windows Evidence Completion Receipt

## Scope

- Candidate worktree: `C:\Users\kingd\AppData\Local\Temp\McpServer-validation\808ec049d56daf214c391beccd6b9d69bb9867c6-20260906T185957Z-319842a9`
- Commit: `808ec049d56daf214c391beccd6b9d69bb9867c6`
- Tree: `5091b64a8d9dade0bfb0b2b0295b93d464e60e1c`
- Configuration: `Release`
- SDK: `10.0.400`
- Evidence root: `C:\Users\kingd\AppData\Local\Temp\McpServer-validation-evidence\808ec049d56daf214c391beccd6b9d69bb9867c6-20260906T191653Z`
- Runtime temp root: `<evidence-root>\runtime-temp`, supplied through `MCP_TEST_TEMP_ROOT`
- Exclusions: no integration, provider, Linux, deployment, synchronization, migration, Windows-service, or WSL actions were part of this receipt.

The candidate had the required commit and tree and was clean before and after the commands: `git status --porcelain=v1 --untracked-files=all` returned zero entries, and both unstaged and staged `git diff --exit-code` checks returned `0`.

## Full Solution Build

Command:

```powershell
dotnet build McpServer.sln --configuration Release --no-restore --property:TreatWarningsAsErrors=true --maxcpucount:1 /p:UseSharedCompilation=false -bl:<evidence-root>\full-solution-release-build.binlog
```

Result: exit `0`, duration `171.589s`, `0` warnings, `0` errors.

- Binlog: `<evidence-root>\full-solution-release-build.binlog`
  - Bytes: `4751195`
  - SHA-256: `D6A987963CEA79882CE0B0EA0949A9CEB718A856DD3611637F24D13475155C24`
- Console log: `<evidence-root>\full-solution-release-build.log`
  - Bytes: `11251`
  - SHA-256: `E4348676D27014D1C7F4CE716CD46DE0D5D1A47456EDE750D936B6CC20FCB586`

## Seven-Project Nuke-Equivalent Gate

Each project ran serially with the exact repository filter and current Release binaries:

```powershell
dotnet test <project> --configuration Release --no-build --no-restore --filter "Category!=AiReview&Category!=Integration" --logger "trx;LogFileName=<project>.trx" --results-directory <evidence-root>\<project>
```

Parsed TRX counters:

- `McpServer.Support.Mcp.Tests`: total `2122`, executed `2122`, passed `2122`, failed `0`, notExecuted `0`, result elements `2122`, definition elements `2103`; SHA-256 `6BBF11454F19B82EB3F0AAFCD91931EA5E80440CDB751C31D9F2D0697D5DF805`.
- `McpServer.Client.Tests`: total `294`, executed `294`, passed `294`, failed `0`, notExecuted `0`, result elements `294`, definition elements `294`; SHA-256 `0506B4CB1846E82A77F448A6D1407EE4CD0263EB34FFF7CDCC3413D31F5871B9`.
- `McpServer.Cqrs.Tests`: total `33`, executed `33`, passed `33`, failed `0`, notExecuted `0`, result elements `33`, definition elements `33`; SHA-256 `9C51A5098EB2A4B225C19C089D4CE8118171135F3C1B4002BBD394BEE3D6A9B5`.
- `McpServer.Launcher.Tests`: total `20`, executed `20`, passed `20`, failed `0`, notExecuted `0`, result elements `20`, definition elements `20`; SHA-256 `960E22318BF136C74CB7CE94BF65BEDEAEE79AA8492E3440A9315420FA033376`.
- `McpServer.McpAgent.Tests`: total `63`, executed `63`, passed `63`, failed `0`, notExecuted `0`, result elements `63`, definition elements `63`; SHA-256 `F3AB88A50DBF823E7BB5BCF20858E0BF28811049A660E1C04A5E602410978428`.
- `McpServer.Repl.Core.Tests`: total `822`, executed `822`, passed `822`, failed `0`, notExecuted `0`, result elements `822`, definition elements `790`; SHA-256 `4C38747094E8C5B7608F20A26E940C4FB57E2CA3394E2BFAB0E3A4259586FBEA`.
- `McpServer.QBAgent.Tests`: total `50`, executed `50`, passed `50`, failed `0`, notExecuted `0`, result elements `50`, definition elements `50`; SHA-256 `F3093E48095779FA3189E1A023873496BDD33F0294930392A3907A051A4AEC18`.

Aggregate: total `3404`, executed `3404`, passed `3404`, failed `0`, notExecuted `0`, result elements `3404`, definition elements `3353`. The lower definition count is directly parsed from `TestDefinitions`; theory cases produce multiple result elements. No NotRun value was inferred.

## Build.Tests Independent Gate

Command:

```powershell
dotnet test tests\Build.Tests\Build.Tests.csproj --configuration Release --no-build --no-restore --logger "trx;LogFileName=Build.Tests.trx" --results-directory <evidence-root>\Build.Tests
```

This gate did not complete. At `2026-09-06T19:41:03.3275574Z`, the outer process had run for `1015.37s`; the console log had not changed since `2026-09-06T19:24:16.4597209Z`. The `dotnet`, `vstest`, `testhost`, and `Build.Tests` processes all showed `0.00s` CPU delta over five seconds. A subsequent 30-second sample ending `2026-09-06T19:42:12.0577688Z` showed unchanged log length, timestamp, and `Build.Tests` CPU (`10.296875s`). There was no child beneath `Build.Tests.exe`.

The run was interrupted after `1103.46s` as a verified non-progressing testhost, and its remaining orphaned `Build.Tests.exe` process was terminated after command-line identity verification. No TRX was produced, so no completed-run total or NotRun count is claimed.

The raw console log records one observed failure before the hang:

- `NukeBuild.Tests.WarningSuppressionValidationTargetTests.Scan_CurrentRepository_HasNoGeneratedMigrationObsoleteWarningPragmas`
- Cause: generated SQLite, SQL Server, and PostgreSQL migration designer/snapshot files contain obsolete-warning pragma pairs rejected by the current-repository scanner.

Raw evidence:

- `<evidence-root>\Build.Tests\Build.Tests.console.log`
  - Bytes: `7513`
  - SHA-256: `6B638A6B2285D7BA79270060B99AF1B3EFF9A3782DDD4ED134DB6087A2CEAA41`
- `<evidence-root>\Build.Tests\Build.Tests.trx`: not created because the testhost did not complete.

## Conclusion

The full Release solution build and the repository-defined seven-project filtered unit gate pass on the exact immutable candidate with complete binlog/TRX evidence. The separately required `Build.Tests` gate does not pass: it records one concrete policy-test failure and then hangs without producing a TRX. This receipt does not treat the Windows evidence program, provider validation, Linux validation, or the broader completion program as complete.
