$ErrorActionPreference = 'Stop'
$wt = 'F:\GitHub\McpServer\.worktrees\sessionlife-p2-contracts-5cb2'
$utc = '20261002T194331Z'
$runId = 'p2-unit-legion-20261002T193045Z'
$tip = '987cd8f39610c9fa301862e3b1d526d23e049469'
$log = Join-Path $wt "docs\receipts\hv\$runId-gate.log"
$hash = (Get-FileHash -LiteralPath $log -Algorithm SHA256).Hash
$bytes = (Get-Item -LiteralPath $log).Length
$dir = Join-Path $wt 'docs\receipts\hv'
$mdPath = Join-Path $dir "$utc-sessionlife-p2-unit-gate-failed.md"
$jsonPath = Join-Path $dir "$utc-sessionlife-p2-unit-gate-failed.json"

$doc = [ordered]@{
    timestampUtc = '2026-10-02T19:43:31Z'
    runId = $runId
    tipSha = $tip
    branch = 'cursor/sessionlife-p2-contracts-5cb2'
    pr = 72
    prUrl = 'https://github.com/sharpninja/McpServer/pull/72'
    verdict = 'FAILED'
    gateExit = 1
    astraStarted = $false
    pluginIntegrationInUnitInventory = $false
    pester = [ordered]@{ total = 242; passed = 236; failed = 6; skipped = 0 }
    nuke = [ordered]@{
        projects = 7
        total = 4297
        passed = 4296
        failed = 1
        pluginIntegrationTestRun = $false
        failedTest = 'McpServer.Support.Mcp.Tests.Services.CliBrainSlotStrategyTests.CompleteAsync_Grok_UsesIsolatedCwdNotConfiguredRepoRoot'
        check = 'incomplete-trx-outcome on McpServer.Support.Mcp.Tests.trx'
    }
    buildTests = [ordered]@{ total = 321; passed = 321; failed = 0; skipped = 0 }
    gateLog = [ordered]@{ path = $log; bytes = $bytes; sha256 = $hash }
    launcherNote = 'First attempt p2-unit-legion-20261002T192840Z died in CaptureSessionLifeUnitGate because %USERPROFILE%\.dotnet\tools\pwsh.exe has process image dotnet.exe. The recorded run moved that shim aside so pwsh resolved to PowerShell 7.6.6, then restored the shim.'
    pesterFails = @(
        'FR-MCP-SESSIONLIFE P2 cache identity metadata and outcomes.covers primary queued and lost outcomes for every session verb (beginTurn primary expected empty string, actual persisted result length 398)'
        'Session lifecycle public wrapper processes.codex exposes exactly one primary receipt and the matching process exit (missing F:\GitHub\McpServer\.worktrees\mcpserver-codex-plugin\Invoke-CodexMcpPlugin.ps1, exit 64)'
        'Session lifecycle public wrapper processes.codex exposes exactly one queued receipt and the matching process exit (same missing script, exit 64)'
        'Session lifecycle public wrapper processes.codex exposes exactly one rejected receipt and the matching process exit (same missing script, exit 64)'
        'Session lifecycle public wrapper processes.codex exposes exactly one lost receipt and the matching process exit (same missing script, exit 64)'
        'Session lifecycle public wrapper processes.codex exposes exactly one unchanged receipt and the matching process exit (same missing script, exit 64)'
    )
}

$json = $doc | ConvertTo-Json -Depth 6
[System.IO.File]::WriteAllText($jsonPath, $json + [Environment]::NewLine, [System.Text.UTF8Encoding]::new($false))

$md = @(
    '# SessionLife P2 unit gate FAILED'
    ''
    '- TimestampUtc: 2026-10-02T19:43:31Z'
    '- Tip: 987cd8f39610c9fa301862e3b1d526d23e049469'
    '- Branch: cursor/sessionlife-p2-contracts-5cb2'
    '- PR: https://github.com/sharpninja/McpServer/pull/72 (left open, not merged)'
    '- RunId: p2-unit-legion-20261002T193045Z'
    '- GATE_EXIT: 1'
    '- Astra: not started'
    '- PluginIntegration: not in the unit inventory (7 selected projects, no PluginIntegration test run)'
    ''
    '## Counts'
    ''
    '- Pester: 236 passed, 6 failed, 0 skipped, total 242'
    '- Nuke: 4296 passed, 1 failed, total 4297 across 7 projects'
    '- Build.Tests: 321 passed, 0 failed, 0 skipped, total 321'
    '- CheckSessionLifeUnitGate: rejected. reason=incomplete-trx-outcome affected=McpServer.Support.Mcp.Tests.trx'
    ''
    '## Gate log'
    ''
    "- Path: $log"
    "- Bytes: $bytes"
    "- SHA256: $hash"
    ''
    '## Pester failures'
    ''
    '1. covers primary queued and lost outcomes for every session verb. beginTurn primary expected an empty string and got a persisted result payload (length 398).'
    '2. codex exposes exactly one primary receipt. Missing F:\GitHub\McpServer\.worktrees\mcpserver-codex-plugin\Invoke-CodexMcpPlugin.ps1 (exit 64, expected 0).'
    '3. codex exposes exactly one queued receipt. Same missing script (exit 64, expected 0).'
    '4. codex exposes exactly one rejected receipt. Same missing script (exit 64, expected 1).'
    '5. codex exposes exactly one lost receipt. Same missing script (exit 64, expected 1).'
    '6. codex exposes exactly one unchanged receipt. Same missing script (exit 64, expected 0).'
    ''
    '## Nuke failure'
    ''
    '- McpServer.Support.Mcp.Tests: 2912 passed, 1 failed, total 2913, outcome Failed.'
    '- Failed test: McpServer.Support.Mcp.Tests.Services.CliBrainSlotStrategyTests.CompleteAsync_Grok_UsesIsolatedCwdNotConfiguredRepoRoot'
    '- Other six unit projects completed with zero failures: Client 304, Cqrs 33, Launcher 20, McpAgent 63, Repl.Core 868, QBAgent 96.'
    ''
    '## Launcher note'
    ''
    'A first attempt, RunId p2-unit-legion-20261002T192840Z, stopped in CaptureSessionLifeUnitGate. The gate prepends %USERPROFILE%\.dotnet\tools, and pwsh.exe there has process image dotnet.exe, so build.ps1 re-exec became dotnet--NoLogo. The recorded run moved that shim aside, used PowerShell 7.6.6, and restored the shim afterward.'
    ''
    'No product remediation and no Astra round were run.'
) -join [Environment]::NewLine
[System.IO.File]::WriteAllText($mdPath, $md + [Environment]::NewLine, [System.Text.UTF8Encoding]::new($false))
Write-Output ('MD=' + $mdPath)
Write-Output ('JSON=' + $jsonPath)
