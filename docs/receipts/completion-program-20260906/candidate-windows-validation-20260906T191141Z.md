# BUG-TRIAGE-139 Immutable Candidate Windows Validation

- Recorded at: `2026-09-06T19:11:41.9896034Z`
- Delegated Codex task: current bounded candidate-validation task
- Requested model: `gpt-5.6-sol`, effort `high`
- Command runner: PowerShell.Mcp 1.14 isolated subagent `sa-4f96637e`
- Result: repository-defined Release Compile and Windows unit Test gates passed
- Scope: immutable prior candidate validation only; no fixes, commits, metadata rewrites, deployment, synchronization, traceability export, package installation target, WSL action, or integration merge was performed

## Immutable Source

- Source repository: `F:\GitHub\McpServer`
- Detached validation worktree: `C:\Users\kingd\AppData\Local\Temp\McpServer-validation\808ec049d56daf214c391beccd6b9d69bb9867c6-20260906T185957Z-319842a9`
- Commit before and after gates: `808ec049d56daf214c391beccd6b9d69bb9867c6`
- Tree before and after gates: `5091b64a8d9dade0bfb0b2b0295b93d464e60e1c`
- Parent: `e19831cd9b52c7bd8c44382ac9a7d1e8e809b02d`
- Required base: `210cb223d81dac6b4045b868e4d7b2712d08b752`
- Base tree: `bfe657bf40ae92046cfa52136d06b79f99bdc2c2`
- Base ancestry check: exit `0`
- Containing branch: `codex/bug-triage-139-remediation`
- Containing annotated tag: `BUG-TRIAGE-139-twenty-fourth-exact-final`
- Tag object: `e68adc4d1e56c12e1b360d9b1376ba8d242b19c1`
- Tag peeled commit: `808ec049d56daf214c391beccd6b9d69bb9867c6`

The C: validation root was normalized with `System.IO.Path.GetFullPath`, required to begin with `C:\Users\kingd\AppData\Local\Temp\McpServer-validation\`, checked as a non-reparse directory, and checked for target nonexistence before worktree creation.

The first worktree attempt used the same contained naming contract but omitted command-scoped Git long-path support. Git failed with exit `128` while materializing deeply nested historical receipt paths and automatically removed both the partial directory and worktree registration. No recursive delete, move, reset, or manual cleanup was used. The successful attempt used:

```powershell
git -c core.longpaths=true -C 'F:\GitHub\McpServer' worktree add --detach '<validation-path>' 808ec049d56daf214c391beccd6b9d69bb9867c6
```

`core.longpaths=true` was command-scoped. The repository configuration remains unset for `core.longpaths`.

## Build Target Inspection

The exact candidate's build sources were inspected before running a gate:

- `build.ps1` launches the Nuke `_build.csproj` through PowerShell Core and `dotnet run`.
- `Compile` depends on `Restore`, enumerates source and test projects except `Build.Tests`, and invokes Release `dotnet build --no-restore` for each project.
- `Test` depends on `Compile`, runs projects ending in `.Tests`, excludes integration, validation, review, and `Build.Tests` projects, and applies `Category!=AiReview&Category!=Integration`.
- `Clean` is ordered before `Restore` only when selected; neither `Compile` nor `Test` depends on `Clean`.
- Neither requested target invokes service deployment, plugin synchronization, traceability export, package publishing, or test-dependency installation.
- Candidate `Directory.Build.props` sets `TreatWarningsAsErrors=true`. It was not overridden or modified.

## Runtime Isolation

Only the documented provider temp variable was set:

```text
MCP_TEST_TEMP_ROOT=C:\Users\kingd\AppData\Local\Temp\McpServer-validation-runtime\808ec049d56daf214c391beccd6b9d69bb9867c6-20260906T190118Z
```

No `TEMP`, `TMP`, `DirectoryBuildPropsPath`, base output, base intermediate output, NuGet package root, or provider connection override was applied. The provider temp directory was empty after the gates. C: free space was `242541916160` bytes before execution and `238725099520` bytes after execution.

## Compile Gate

Exact command:

```powershell
& 'C:\Users\kingd\.cache\codex-runtimes\codex-primary-runtime\dependencies\native\powershell\pwsh.exe' -NoProfile -NonInteractive -File '.\build.ps1' Compile --configuration Release
```

- Started: `2026-09-06T19:01:46.1454118Z`
- Ended: `2026-09-06T19:04:14.8569582Z`
- Duration: `148.712` seconds
- Exit code: `0`
- Nuke targets: Restore `Succeeded`; Compile `Succeeded`
- Project build invocations: `45`
- `Build succeeded.` summaries: `45`
- Zero-warning summaries: `45`
- Zero-error summaries: `45`
- Failure lines: `0`
- Console log: `C:\Users\kingd\AppData\Local\Temp\McpServer-validation-results\808ec049d56daf214c391beccd6b9d69bb9867c6-20260906T190118Z\nuke-compile-release.console.txt`
- Console length: `83606` bytes
- Console SHA-256: `82C313BCB7FE12174FE8E5EC734AB736FC4F5ACD036A9BE5FE4D48BDC464B127`

An earlier attempted command named the nonexistent path `C:\Program Files\PowerShell\7\pwsh.exe`. PowerShell rejected it before `build.ps1` launched. The gate above used the actual PowerShell.Mcp host runtime and is the only Compile result claimed.

## Test Gate

Exact command:

```powershell
& 'C:\Users\kingd\.cache\codex-runtimes\codex-primary-runtime\dependencies\native\powershell\pwsh.exe' -NoProfile -NonInteractive -File '.\build.ps1' Test --configuration Release
```

- Started: `2026-09-06T19:04:26.6375555Z`
- Ended: `2026-09-06T19:09:15.4574313Z`
- Duration: `288.820` seconds
- Exit code: `0`
- Nuke targets: Restore `Succeeded`; Compile `Succeeded`; Test `Succeeded`
- Test project invocations: `7`
- Result summaries: `7`
- Passed: `3404`
- Failed: `0`
- Skipped: `0`
- Console log: `C:\Users\kingd\AppData\Local\Temp\McpServer-validation-results\808ec049d56daf214c391beccd6b9d69bb9867c6-20260906T190118Z\nuke-test-release.console.txt`
- Console length: `80179` bytes
- Console SHA-256: `2A7F32C252D8D65907203A4706B201DD0E679A16316C2BB37FAE0774DB111C04`

Per-project console counts:

- `McpServer.Support.Mcp.Tests`: 2122 passed, 0 failed, 0 skipped
- `McpServer.Client.Tests`: 294 passed, 0 failed, 0 skipped
- `McpServer.Cqrs.Tests`: 33 passed, 0 failed, 0 skipped
- `McpServer.Launcher.Tests`: 20 passed, 0 failed, 0 skipped
- `McpServer.McpAgent.Tests`: 63 passed, 0 failed, 0 skipped
- `McpServer.Repl.Core.Tests`: 822 passed, 0 failed, 0 skipped
- `McpServer.QBAgent.Tests`: 50 passed, 0 failed, 0 skipped

The Nuke Test target does not configure a TRX logger. Its `TestResults` directory contains no current-run files, and no current-run `.trx` was emitted. Therefore an explicit `NotRun` counter is not available from a TRX artifact. The console contains seven test invocations, seven result summaries, and no `No test matches`, `No test is available`, `not run`, or `not executed` line. `NotRun=0` is not asserted. The 141 `.trx` files elsewhere in the checkout are committed historical receipts and were not attributed to this run.

No current-run `.binlog` was emitted because the Nuke Compile and Test targets do not enable binary logging. Binlog count for this run is `0`.

## Post-Gate Integrity

- `git rev-parse HEAD`: `808ec049d56daf214c391beccd6b9d69bb9867c6`
- `git rev-parse HEAD^{tree}`: `5091b64a8d9dade0bfb0b2b0295b93d464e60e1c`
- `git status --porcelain=v1 --untracked-files=all`: empty
- `git diff --exit-code`: `0`
- `git diff --cached --exit-code`: `0`

The Windows Compile and repository-defined unit Test gates pass for the immutable candidate. This receipt does not claim integration coverage, native Linux/FUSE/non-root coverage, explicit NotRun evidence, merge readiness, deployment readiness, or completion of BUG-TRIAGE-139.
