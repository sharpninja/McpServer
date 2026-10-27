# SessionLife P2 unit gate FAILED

- TimestampUtc: 2026-10-02T19:43:31Z
- Tip: 987cd8f39610c9fa301862e3b1d526d23e049469
- Branch: cursor/sessionlife-p2-contracts-5cb2
- PR: https://github.com/sharpninja/McpServer/pull/72 (left open, not merged)
- RunId: p2-unit-legion-20261002T193045Z
- GATE_EXIT: 1
- Astra: not started
- PluginIntegration: not in the unit inventory (7 selected projects, no PluginIntegration test run)

## Counts

- Pester: 236 passed, 6 failed, 0 skipped, total 242
- Nuke: 4296 passed, 1 failed, total 4297 across 7 projects
- Build.Tests: 321 passed, 0 failed, 0 skipped, total 321
- CheckSessionLifeUnitGate: rejected. reason=incomplete-trx-outcome affected=McpServer.Support.Mcp.Tests.trx

## Gate log

- Path: F:\GitHub\McpServer\.worktrees\sessionlife-p2-contracts-5cb2\docs\receipts\hv\p2-unit-legion-20261002T193045Z-gate.log
- Bytes: 784962
- SHA256: FA0A7A32FF9B72DF30A5829BA437203305511999B8E93F1508B871B87E33C87B

## Pester failures

1. covers primary queued and lost outcomes for every session verb. beginTurn primary expected an empty string and got a persisted result payload (length 398).
2. codex exposes exactly one primary receipt. Missing F:\GitHub\McpServer\.worktrees\mcpserver-codex-plugin\Invoke-CodexMcpPlugin.ps1 (exit 64, expected 0).
3. codex exposes exactly one queued receipt. Same missing script (exit 64, expected 0).
4. codex exposes exactly one rejected receipt. Same missing script (exit 64, expected 1).
5. codex exposes exactly one lost receipt. Same missing script (exit 64, expected 1).
6. codex exposes exactly one unchanged receipt. Same missing script (exit 64, expected 0).

## Nuke failure

- McpServer.Support.Mcp.Tests: 2912 passed, 1 failed, total 2913, outcome Failed.
- Failed test: McpServer.Support.Mcp.Tests.Services.CliBrainSlotStrategyTests.CompleteAsync_Grok_UsesIsolatedCwdNotConfiguredRepoRoot
- Other six unit projects completed with zero failures: Client 304, Cqrs 33, Launcher 20, McpAgent 63, Repl.Core 868, QBAgent 96.

## Launcher note

A first attempt, RunId p2-unit-legion-20261002T192840Z, stopped in CaptureSessionLifeUnitGate. The gate prepends %USERPROFILE%\.dotnet\tools, and pwsh.exe there has process image dotnet.exe, so build.ps1 re-exec became dotnet--NoLogo. The recorded run moved that shim aside, used PowerShell 7.6.6, and restored the shim afterward.

No product remediation and no Astra round were run.
