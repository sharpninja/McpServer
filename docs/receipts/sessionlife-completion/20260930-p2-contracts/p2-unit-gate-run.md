# P2 Cumulative Unit Gate

Recorded from run `p2-unit-20260930T010500Z` on `cursor/sessionlife-p2-contracts-5cb2`.

The gate is red. `hvAgreeClaimed` is false. Items 1-5 stay green on the earlier SessionLog suites. `history-r1` was not rewritten. The 35 batch TODOs stay open. P3 through P7 were not started. `AGENTS-README-FIRST.yaml` is absent (`MCP_UNTRUSTED`).

## What ran

`tools/validation/Invoke-SessionLifeUnitGate.ps1 -RunId p2-unit-20260930T010500Z`

- PowerShell `7.6.6`, Pester `6.2.0`, .NET SDK `10.0.401`, Nuke `9.0.4`.
- `CaptureSessionLifeUnitGate` wrote `TestResults/<id>/source-manifest.json` before the suites.
- Pester ran the whole `plugins/core/test-fixtures/pester` directory. NUnit XML is `pester/results.xml`. Native counters are `pester/native-run.json`.
- Nuke `Test --test-run-id` wrote `unit/selected-projects.json` and one TRX per selected project. The script does not keep its own project list.
- `dotnet test tests/Build.Tests/Build.Tests.csproj -c Debug --filter Category!=Integration` wrote `build-tests/Build.Tests.trx`.
- `CheckSessionLifeUnitGate` called `ValidateSessionLifeUnitGate` with scope `unit`.
- `MigrationIntegrationTests` has the same inventory producer when `--test-run-id` is set. This unit script does not invoke that target.

The evidence Build.Tests invocation also passed `--blame-hang --blame-hang-timeout 3min`. The suite finished in 22s (310 passed, 4 failed, 0 skipped). The blame collector then aborted the host after 3 minutes of inactivity and wrote two hang dumps. Those dumps were deleted and are not retained. The committed script does not pass those flags. The TRX below is the report.

Raw files live at `TestResults/p2-unit-20260930T010500Z` and are gitignored by `[Tt]est[Rr]esult*/`. SHA-256 values below are of those files. Every failed test name and first error line is in `p2-unit-gate-failures.json`.

## Validator

`CheckSessionLifeUnitGate` failed in under one second:

`System.IO.InvalidDataException: Session-life gate rejected the run. reason=incomplete-trx-outcome affected=McpServer.Support.Mcp.Tests|unit/McpServer.Support.Mcp.Tests/McpServer.Support.Mcp.Tests.trx`

Inventory validation runs before command results and Pester. `McpServer.Support.Mcp.Tests` is the first selected project, and its TRX `outcome` is `Failed` because 70 tests failed. The validator requires `Completed`. The same `Failed` outcome is on `McpServer.McpAgent.Tests`, `McpServer.PluginIntegration.Tests`, and `Build.Tests`. Counters on every TRX have `notExecuted=0`. Pester has `skipped=0`, `ignored=0`, `not-run=0`, and `inconclusive=0`. The gate is red because of failures. It is not red because of skips.

Command exits in `command-results.json` (SHA-256 `e45b1c689c722610c83856382947407ff3bf453451bd63dc3129bd208f99e998`):

- `pester` exit 1, `2026-09-30T01:01:22.3590467+00:00` to `2026-09-30T01:02:33.9008345+00:00`
- `nuke-test` exit 255, `2026-09-30T01:02:33.9047945+00:00` to `2026-09-30T01:05:36.1489632+00:00`
- `build-tests` exit 1, `2026-09-30T01:05:36.1504616+00:00` to `2026-09-30T01:09:03.0384672+00:00`

Nuke threw `Session-life unit test projects failed: McpServer.Support.Mcp.Tests, McpServer.McpAgent.Tests, McpServer.PluginIntegration.Tests` after writing every selected project's TRX. The script exited 1.

## Selected unit inventory

`unit/selected-projects.json` SHA-256 `708a0d09b93707e45cc2c01ef99dc375a3d46fbc50299250b55bbf0d23b4ee3e`. `source-manifest.json` SHA-256 `530ac10141ebdff71ff638bb957b0b5f5e292681830817ce2f624d4f94b13bad`.

- `McpServer.Client.Tests` Completed, 301/301. SHA-256 `995e54d2eb5022f66cd47bc7f5afce383bd8471148f08112c1d1dd1c34bbffa2`.
- `McpServer.Cqrs.Tests` Completed, 33/33. SHA-256 `a4189cd2687d0ea7cdcdd71a2c3bc8a55f308e66526e7f548ec08133d40f9c47`.
- `McpServer.Launcher.Tests` Completed, 20/20. SHA-256 `5a5d042bfdc201e9f900691a4bb42d542fc7582707125c904ee50eff55b7c948`.
- `McpServer.QBAgent.Tests` Completed, 90/90. SHA-256 `fab04b45e83aff71ff0486527c6ec0ef91cfffb3891f9075d284479053ade774`.
- `McpServer.Repl.Core.Tests` Completed, 866/866. SHA-256 `1b3409dd5ccc8523f73ad8d17bf715c24ef787510cc772734a28eb4e8659fa97`.
- `McpServer.McpAgent.Tests` Failed, executed 63, passed 56, failed 7, notExecuted 0. SHA-256 `cebfc1a914633dc7cdd6fd4079816be79bf0ed8b052a61aebe28a7f0274055b8`.
- `McpServer.PluginIntegration.Tests` Failed, executed 94, passed 13, failed 81, notExecuted 0. SHA-256 `a33975d30b1279e62440fe6ed37bdb2f72bc3935da6a14ad318163a815dc34ae`.
- `McpServer.Support.Mcp.Tests` Failed, executed 2893, passed 2823, failed 70, notExecuted 0. SHA-256 `f1f05c743af7a13ddb221626ac40697a1fc2a8883d18fa064ecb84168f4bd1e4`.

Nuke unit totals: executed 4360, passed 4202, failed 158, notExecuted 0.

## Build.Tests

`build-tests/Build.Tests.trx` outcome `Failed`, total 314, executed 314, passed 310, failed 4, notExecuted 0. SHA-256 `c5d69ec5b5eddd5a63bcc9badf619542d7116fa33df0b5159713cea26b97616c`.

The four failures:

- `NukeBuild.Tests.HandoffD3D5OverlayTests.D3_ValidateTraceability_ShippedValidator_PassesOnLiveDocsProject` — `C3 FR gaps: mapping=[FR-MCP-SERVICEUPDATE-001] matrix=[FR-MCP-SERVICEUPDATE-001]`
- `NukeBuild.Tests.DocsSyncG8OverlayTests.G8_ValidateTraceability_ShippedValidator_PassesOnLiveDocsProject` — the same `FR-MCP-SERVICEUPDATE-001` gap
- `NukeBuild.Tests.SyncAgentPluginsChecksumTests.SyncAgentPlugins_OfficialPluginLibChecksumsMatchCanonicalCore` — `mcpserver-codex-plugin`, `mcpserver-claude-code-plugin`, `mcpserver-copilot-plugin`, `mcpserver-cline-plugin`, and `mcpserver-grok-plugin` roots are missing
- `NukeBuild.Tests.DocsSyncG8OverlayTests.G8_SyncAgentPlugins_OfficialPluginLibChecksumsMatchCanonicalCore` — the same five plugin roots are missing

The SessionLifeUnitGate facts inside this project are part of the 310 passes. The earlier filtered 95/0/0 run is unchanged and is not this cumulative gate.

## Pester

`pester/native-run.json` SHA-256 `25c0080feedb9c7b0619b378e5a1e2a2efb588eb97e1aa80159a798a83beab54`: total 193, passed 179, failed 14, skipped 0, inconclusive 0, notRun 0, failedBlocks 1, failedContainers 0.

`pester/results.xml` SHA-256 `f0651641b68820a4263b383d4f988224f6022456531bd523dcc860dede3b7b81`. Root attributes: total 193, failures 14, errors 0, not-run 0, inconclusive 0, ignored 0, skipped 0, invalid 0, date `2026-09-30`, time `01:01:22`.

The 14 failed cases:

- `TEST-HANDOFF-006` `PluginHandoffSkill_InvokeIngestGetApprove_UsesDocumentedWorkflowMethods` — parent block failed because `mcpserver-grok-plugin/skills/handoff/SKILL.md` is missing. This is the failed block.
- `TEST-MCP-PLUGIN-PSONLY-002 rejects forbidden Bash and Node runtime files from shipped plugin packages` — `Path` is empty because the staged package is absent.
- `TEST-MCP-PLUGIN-PSONLY-003 fails closed before MCP work when the runtime is refused` — expected 0, got 64.
- `TEST-MCP-PLUGIN-PSONLY-002 generated wrappers invoke PowerShell hook entrypoint` — expected 0, got 64.
- `TEST-MCP-PLUGIN-PSONLY-002 status entrypoint returns JSON status without mutation` — expected 0, got 64.
- `TEST-MCP-BUGTRIAGE-051 marker refresh writes sessionId before reporting verified` — no `AGENTS-README-FIRST.yaml` walking up from `/workspace`.
- `TEST-MCP-TRANSCRIPT-010` — missing `plugins/core/.staged-plugin/skills/session/SKILL.md`.
- `TEST-MCP-YAML-MUTATION-001` — missing `plugins/core/.staged-plugin/skills`.
- `TEST-MCP-BUGTRIAGE-050` — missing `plugins/core/.staged-plugin/skills/triage/SKILL.md`.
- `TEST-MCP-PLUGIN-PSONLY-002 synced manifest contains PowerShell runtime files only` — expected `$true`, got `$false`.
- `TEST-MCP-BUGTRIAGE-027 core integrity checker exits 0 on success` — expected 0, got 1.
- `TEST-MCP-TEMPVOL-001` sets TEMP with `subst` — `subst` is not present on this Linux host.
- `TEST-MCP-TEMPVOL-001` does not mutate TEMP when the workspace temp directory cannot be created — expected `$false`, got `$true`.
- `TEST-MCP-TRIAGEPLUGIN-004 PersistTurn.SubmitAsyncChildTimeout_ReturnsDegradedQueued` — expected `$false`, got `$true`. The fact drives `mcpserver-repl.cmd` with `REPL_TIMEOUT=1`. That `.cmd` hang does not run on Linux, so the child fails immediately.

## Other TRX failures

`McpServer.McpAgent.Tests`, all 7: `OptionsValidationException: WorkspacePath must be fully qualified when provided.`

- `McpSessionIdentifierFactoryTests.AddMcpServerMcpAgent_RegistersIdentifierFactory`
- `ServiceCollectionExtensionsTests.AddMcpServerMcpAgent_RegistersHostedAgentFactory`
- `ServiceCollectionExtensionsTests.AddMcpServerMcpAgent_UsesPreconfiguredOptions`
- `ServiceCollectionExtensionsTests.AddMcpServerMcpAgent_AllowsBearerTokenAuthentication`
- `ServiceCollectionExtensionsTests.AddMcpServerMcpAgent_RegistersAcidTightlyCoupledProfile`
- `ServiceCollectionExtensionsTests.AddMcpServerMcpAgent_AllowsMissingAuthenticationWhenDisabled`
- `ServiceCollectionExtensionsTests.AddMcpServerMcpAgent_RegistersScaffoldedHostedAgent`

`McpServer.PluginIntegration.Tests`, 81 failures: 80 throw `Plugin repository root is missing: mcpserver-codex-plugin`. One fails with `P20 harness log is missing: harness.log` (`PluginNativeSuiteReceiptTests.PluginNativeSuite_EachOfficialPlugin_FailedZeroSkippedZero`). Every name is in the failure index.

`McpServer.Support.Mcp.Tests`, 70 failures. These are Linux and checkout assertions (missing FUSE/`fusermount3`, atomic export-root `DirectoryNotFoundException`, missing BUG-TRIAGE-139 ledger, missing sibling `mcpserver-grok-plugin` handoff skill, and assorted `Assert.*` failures). They are not P2 contract regressions, and this slice does not edit them. Every name and first error line is in `p2-unit-gate-failures.json`.

## Not claimed

P2 exit is not met. Independent Grok HV is not claimed. The parent launches that review when a later cumulative run is green.
