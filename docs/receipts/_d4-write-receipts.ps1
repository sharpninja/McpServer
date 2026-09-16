#Requires -Version 7.0
[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$outDir = 'F:\GitHub\McpServer\docs\receipts\_d4-20260822T141644Z'
$stamp = '20260822T141644Z'
$receiptMd = "F:\GitHub\McpServer\docs\receipts\d4-handoff-gate-$stamp.md"
$receiptJson = "F:\GitHub\McpServer\docs\receipts\d4-handoff-gate-$stamp.json"

function Get-FileSha256 {
    param([string]$Path)
    if (-not (Test-Path -LiteralPath $Path)) { return $null }
    return (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash
}

$cmd1 = Get-Content -LiteralPath (Join-Path $outDir '01-client-tests.json') -Raw | ConvertFrom-Json
$cmd2 = Get-Content -LiteralPath (Join-Path $outDir '02-support-mcp-tests.json') -Raw | ConvertFrom-Json

$failure = [ordered]@{
    testName = 'McpServer.Support.Mcp.Tests.Products.GetProductEffectiveRequirementsQueryHandlerTests.HandleAsync_NullLayerKey_UsesPersistedCurrentLayerAfterExternalUpdate'
    expected = 'layer-2'
    actual   = 'layer-1'
    file     = 'tests/McpServer.Support.Mcp.Tests/Products/GetProductEffectiveRequirementsQueryHandlerTests.cs'
    line     = 260
    message  = 'Assert.Equal() Failure: Strings differ. Expected "layer-2" Actual "layer-1".'
}

$notRun = @(
    [ordered]@{ index = 3; name = 'repl-core-tests'; exactCommand = 'dotnet test tests\McpServer.Repl.Core.Tests\McpServer.Repl.Core.Tests.csproj'; exitCode = $null; passed = $null; failed = $null; skipped = $null; status = 'not_run' }
    [ordered]@{ index = 4; name = 'support-mcp-integration'; exactCommand = 'dotnet test tests\McpServer.Support.Mcp.IntegrationTests\McpServer.Support.Mcp.IntegrationTests.csproj'; exitCode = $null; passed = $null; failed = $null; skipped = $null; status = 'not_run' }
    [ordered]@{ index = 5; name = 'repl-integration'; exactCommand = 'dotnet test tests\McpServer.Repl.IntegrationTests\McpServer.Repl.IntegrationTests.csproj'; exitCode = $null; passed = $null; failed = $null; skipped = $null; status = 'not_run' }
    [ordered]@{ index = 6; name = 'compile'; exactCommand = '.\build.ps1 Compile'; exitCode = $null; passed = $null; failed = $null; skipped = $null; status = 'not_run' }
    [ordered]@{ index = 7; name = 'test'; exactCommand = '.\build.ps1 Test'; exitCode = $null; passed = $null; failed = $null; skipped = $null; status = 'not_run' }
    [ordered]@{ index = 8; name = 'validate-traceability'; exactCommand = '.\build.ps1 ValidateTraceability'; exitCode = $null; passed = $null; failed = $null; skipped = $null; status = 'not_run' }
    [ordered]@{ index = 9; name = 'sync-agent-plugins'; exactCommand = '.\build.ps1 SyncAgentPlugins'; exitCode = $null; passed = $null; failed = $null; skipped = $null; status = 'not_run' }
)

$logFiles = @(
    '01-client-tests.json'
    '01-client-tests.log'
    '01-client-tests.trx'
    '02-support-mcp-tests.json'
    '02-support-mcp-tests.log'
    '02-support-mcp-tests.trx'
    'ids.json'
    'git-status-before.txt'
    'git-status-after.txt'
    'session-dialog-start.json'
    'tools-list.json'
)
$shas = [ordered]@{}
foreach ($f in $logFiles) {
    $p = Join-Path $outDir $f
    $shas[$f] = Get-FileSha256 -Path $p
}

$summary = [ordered]@{
    timestampUtc = [DateTimeOffset]::UtcNow.ToString('o')
    agent        = 'GrokCode'
    sessionId    = 'GrokCode-20260822T141644Z-pluginhandoff-d4-gate'
    requestId    = 'req-20260822T141644Z-001-pluginhandoff-d4-gate'
    turnId       = 43015
    todoId       = 'PLAN-PLUGINHANDOFF-001'
    plan         = 'docs/plans/PLAN-PLUGINHANDOFF-001.md'
    phase        = 'D4'
    d4Complete   = $false
    todosLeftDoneFalse = @(
        'PLAN-PLUGINHANDOFF-001'
        'MCP-HANDOFF-001'
        'MCP-HANDOFFPLAN-001'
        'MCP-HANDOFFREVIEW-001'
    )
    overall = [ordered]@{
        failedZero  = $false
        skippedZero = $true
        allGreen    = $false
        failed      = 1
        skipped     = 0
        commandsRun = 2
        commandsTotal = 9
        stoppedAfter = 2
        stoppedReason = 'stop after index 2 name support-mcp-tests: exit=1 failed=1 skipped=0'
    }
    commands = @(
        [ordered]@{
            index = 1
            name = 'client-tests'
            exactCommand = $cmd1.exactCommand
            exitCode = [int]$cmd1.exitCode
            passed = [int]$cmd1.passed
            failed = [int]$cmd1.failed
            skipped = [int]$cmd1.skipped
            total = [int]$cmd1.total
            status = 'ran'
            logSha256 = $cmd1.logSha256
            trxSha256 = $cmd1.trxSha256
        }
        [ordered]@{
            index = 2
            name = 'support-mcp-tests'
            exactCommand = $cmd2.exactCommand
            exitCode = [int]$cmd2.exitCode
            passed = [int]$cmd2.passed
            failed = [int]$cmd2.failed
            skipped = [int]$cmd2.skipped
            total = [int]$cmd2.total
            status = 'ran'
            logSha256 = $cmd2.logSha256
            trxSha256 = $cmd2.trxSha256
            failure = $failure
        }
    ) + $notRun
    artifactShas = $shas
    trust = [ordered]@{
        markerSignature = $true
        healthNonceSent = '93a9873cb9d44c57b57c873436a3f6ba'
        healthNonceEcho = '93a9873cb9d44c57b57c873436a3f6ba'
        healthNonceMatch = $true
        healthStatus = 'Healthy'
        pluginVersion = '1.105.0'
        pluginName = 'mcpserver-grok-plugin'
    }
}

$summary | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath (Join-Path $outDir 'summary.json') -Encoding utf8
Copy-Item -LiteralPath (Join-Path $outDir 'summary.json') -Destination $receiptJson -Force

$md = @"
# D4 HANDOFFPLAN gate for PLAN-PLUGINHANDOFF-001

TimestampUtc: 2026-08-22T14:23:16Z
Agent: GrokCode
Session: GrokCode-20260822T141644Z-pluginhandoff-d4-gate
Turn: req-20260822T141644Z-001-pluginhandoff-d4-gate
TurnId: 43015
TODO: PLAN-PLUGINHANDOFF-001 (not marked done)
Related TODOs left done:false: MCP-HANDOFF-001, MCP-HANDOFFPLAN-001, MCP-HANDOFFREVIEW-001
Plan: docs/plans/PLAN-PLUGINHANDOFF-001.md section 8 D4
Phase: D4 gate only. Stopped after first Failed>0. No D5 Codex. No Phase E/F/G. No TODO done flip. No commit.

## Trust bootstrap

- Workspace: F:\GitHub\McpServer
- Marker: F:\GitHub\McpServer\AGENTS-README-FIRST.yaml
- Test-MarkerSignature: True
- Health nonce ``93a9873cb9d44c57b57c873436a3f6ba`` echoed exactly. status Healthy. storage reachable. version 1.4.30+ee89cd63f6d16aa43d8e8dfac2388246c6ba39f8
- Plugin: mcpserver-grok-plugin version 1.105.0 from F:\GitHub\mcpserver-grok-plugin\.version and .claude-plugin\plugin.json
- Native MCP sessionlog_open created=true for GrokCode-20260822T141644Z-pluginhandoff-d4-gate
- Native MCP sessionlog_begin_turn success turnId=43015 status=in_progress

## Verdict

D4 is NOT complete.

Overall Failed 0 Skipped 0: no.

Stopped after command 2 because Failed=1. Commands 3-9 were not run.

## Commands

1. ``dotnet test tests\McpServer.Client.Tests\McpServer.Client.Tests.csproj``
   - exit 0; Passed 284; Failed 0; Skipped 0; Total 284
   - log SHA256: $($cmd1.logSha256)
   - trx SHA256: $($cmd1.trxSha256)

2. ``dotnet test tests\McpServer.Support.Mcp.Tests\McpServer.Support.Mcp.Tests.csproj``
   - exit 1; Passed 2111; Failed 1; Skipped 0; Total 2112
   - log SHA256: $($cmd2.logSha256)
   - trx SHA256: $($cmd2.trxSha256)
   - Failed test: HandleAsync_NullLayerKey_UsesPersistedCurrentLayerAfterExternalUpdate
   - Expected: layer-2; Actual: layer-1
   - File: tests/McpServer.Support.Mcp.Tests/Products/GetProductEffectiveRequirementsQueryHandlerTests.cs line 260

3. ``dotnet test tests\McpServer.Repl.Core.Tests\McpServer.Repl.Core.Tests.csproj``
   - not run (gate stopped)

4. ``dotnet test tests\McpServer.Support.Mcp.IntegrationTests\McpServer.Support.Mcp.IntegrationTests.csproj``
   - not run (gate stopped)

5. ``dotnet test tests\McpServer.Repl.IntegrationTests\McpServer.Repl.IntegrationTests.csproj``
   - not run (gate stopped)

6. ``.\build.ps1 Compile``
   - not run (gate stopped)

7. ``.\build.ps1 Test``
   - not run (gate stopped)

8. ``.\build.ps1 ValidateTraceability``
   - not run (gate stopped)

9. ``.\build.ps1 SyncAgentPlugins``
   - not run (gate stopped)

## Failure detail (command 2)

Console excerpt:

- Failed McpServer.Support.Mcp.Tests.Products.GetProductEffectiveRequirementsQueryHandlerTests.HandleAsync_NullLayerKey_UsesPersistedCurrentLayerAfterExternalUpdate
- Assert.Equal() Failure: Strings differ
- Expected: "layer-2"
- Actual: "layer-1"
- Failed!  - Failed: 1, Passed: 2111, Skipped: 0, Total: 2112

TRX: docs/receipts/_d4-20260822T141644Z/02-support-mcp-tests.trx
Log: docs/receipts/_d4-20260822T141644Z/02-support-mcp-tests.log

## TODO state (MCP todo_get, not TODO.yaml)

Before the gate: PLAN-PLUGINHANDOFF-001, MCP-HANDOFF-001, MCP-HANDOFFPLAN-001, MCP-HANDOFFREVIEW-001 all done=false. No todo_update was issued.

## Artifacts

Directory: docs/receipts/_d4-20260822T141644Z/
JSON twin: docs/receipts/d4-handoff-gate-20260822T141644Z.json
"@

Set-Content -LiteralPath $receiptMd -Value $md -Encoding utf8
$summary.receiptMdSha256 = Get-FileSha256 -Path $receiptMd
$summary.receiptJsonSha256 = Get-FileSha256 -Path $receiptJson
$summary | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath $receiptJson -Encoding utf8
Copy-Item -LiteralPath $receiptJson -Destination (Join-Path $outDir 'summary.json') -Force

Write-Output ('RECEIPT_MD=' + $receiptMd)
Write-Output ('RECEIPT_JSON=' + $receiptJson)
Write-Output ('RECEIPT_MD_SHA=' + (Get-FileSha256 $receiptMd))
Write-Output ('RECEIPT_JSON_SHA=' + (Get-FileSha256 $receiptJson))
Write-Output ('ALL_GREEN=False')
