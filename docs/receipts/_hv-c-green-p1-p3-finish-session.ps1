#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$out = 'F:\GitHub\McpServer\docs\receipts\_hv-c-green-p1-p3'
$plugin = 'F:\GitHub\mcpserver-grok-plugin\lib\Invoke-McpPlugin.ps1'
Set-Location -LiteralPath 'F:\GitHub\McpServer'
$env:MCP_WORKSPACE_PATH = 'F:\GitHub\McpServer'
$env:PLUGIN_AGENT_NAME = 'GrokSubagentHostile'
$env:MCP_AGENT_NAME = 'GrokSubagentHostile'
$env:GROK_PLUGIN_ROOT = 'F:\GitHub\mcpserver-grok-plugin'
$env:MCP_PLUGIN_ROOT = 'F:\GitHub\mcpserver-grok-plugin'

$agent = 'GrokSubagentHostile'
$sessionId = 'GrokSubagentHostile-20260821T234007Z-c-green-p1-p3'
$requestId = 'req-20260821T234007Z-001-hostile-c-green-p1-p3'
$now = [DateTime]::UtcNow.ToString('o')

function Invoke-Plugin {
    param([string]$Method, [hashtable]$Params, [string]$Name)
    $outFile = Join-Path $out ($Name + '.txt')
    $errFile = Join-Path $out ($Name + '.err.txt')
    try {
        $result = & $plugin -Command Invoke -Method $Method -ParamsObject $Params -WorkspacePath 'F:\GitHub\McpServer' -TimeoutSeconds 120 2> $errFile
        if ($null -eq $result) { $result = '' }
        if ($result -isnot [string]) { $result = ($result | Out-String) }
        Set-Content -LiteralPath $outFile -Value $result -Encoding utf8
        Write-Output ('OK ' + $Name)
    } catch {
        Set-Content -LiteralPath $outFile -Value ('ERROR ' + $_.Exception.ToString()) -Encoding utf8
        Write-Output ('FAIL ' + $Name + ' ' + $_.Exception.Message)
        throw
    }
}

$receiptMd = Get-Item -LiteralPath 'F:\GitHub\McpServer\docs\receipts\hostile-validator-20260821T234422Z.md'
$receiptJson = Get-Item -LiteralPath 'F:\GitHub\McpServer\docs\receipts\hostile-validator-20260821T234422Z.json'
$proof = [ordered]@{
    MdExists = $true
    MdLength = $receiptMd.Length
    MdLastWriteTimeUtc = $receiptMd.LastWriteTimeUtc.ToString('o')
    JsonExists = $true
    JsonLength = $receiptJson.Length
    JsonLastWriteTimeUtc = $receiptJson.LastWriteTimeUtc.ToString('o')
}
$proof | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $out 'receipt-on-disk.json') -Encoding utf8

$dialogItems = @(
    @{ timestamp = $now; role = 'model'; category = 'observation'; content = 'add-profile executed. 18 non-skill profile files read. Work class 1. Surfaces A+B+C+D apply. Independent re-run Build.Tests PluginSessionLogIntegrationTargetTests Passed 3 Failed 0 Skipped 0. PluginIntegration.Tests Passed 1 Failed 0 Skipped 0.' }
    @{ timestamp = $now; role = 'model'; category = 'decision'; content = 'Decision: score C-green-P1-P3 from independently re-run tests, live todo_get, file timestamps versus C-red-P1 AGREE, and plugin Test-MarkerSignature, not implementer chat. Consequence: AGREE only if A+B+C+D all PASS. Alternatives rejected: homemade HMAC false as MCP_UNTRUSTED; requiring P4-P20 or PLAN done; treating missing TRX skipped attribute as skipped tests.' }
    @{ timestamp = $now; role = 'model'; category = 'observation'; content = 'C-red-P1 AGREE on disk. Green files after 23:25:45Z. Catalog has Cline v2. PLAN and PLUGININT done false. FailIfPluginSessionLogSkipped treats skip as fail. Plugin signature true. Health nonce echoed.' }
)

Invoke-Plugin -Method 'client.SessionLog.AppendDialogAsync' -Params @{
    agent = $agent
    sessionId = $sessionId
    requestId = $requestId
    items = $dialogItems
} -Name 'sl-dialog'

$actions = @(
    @{ order = 1; description = 'Executed add-profile; read 18 profile markdown files'; type = 'read'; status = 'completed'; filePath = 'C:\Users\kingd\.claude\profile\PROFILE.md' }
    @{ order = 2; description = 'Health nonce 5558b6438b2e489fb0dc93b5e6619462 echoed; plugin Test-MarkerSignature true; BeginTurnAsync turnId 42804'; type = 'read'; status = 'completed'; filePath = 'F:\GitHub\McpServer\AGENTS-README-FIRST.yaml' }
    @{ order = 3; description = 'Re-ran PluginSessionLogIntegrationTargetTests Passed 3 Failed 0 Skipped 0 and PluginIntegration.Tests Passed 1 Failed 0 Skipped 0'; type = 'test'; status = 'completed'; filePath = 'tests/Build.Tests/PluginSessionLogIntegrationTargetTests.cs' }
    @{ order = 4; description = 'Live todo_get PLAN-PLUGINHANDOFF-001 done false; MCP-PLUGININT-001 done false'; type = 'read'; status = 'completed'; filePath = 'docs/receipts/_hv-c-green-p1-p3/todo-plan-pluginhandoff-001.txt' }
    @{ order = 5; description = 'Wrote hostile receipt pair 20260821T234422Z OverallVerdict AGREE'; type = 'create'; status = 'completed'; filePath = 'docs/receipts/hostile-validator-20260821T234422Z.md' }
    @{ order = 6; description = 'Chose plugin Test-MarkerSignature over homemade HMAC false-negative; did not require P4-P20'; type = 'design_decision'; status = 'completed'; filePath = '' }
)

Invoke-Plugin -Method 'client.SessionLog.PatchTurnAsync' -Params @{
    agent = $agent
    sessionId = $sessionId
    requestId = $requestId
    turn = @{
        requestId = $requestId
        queryTitle = 'Hostile validate C-green-P1-P3 plugin sessionlog harness'
        actions = $actions
        designDecisions = @(
            'Score C-green-P1-P3 from independently re-run tests, live MCP store, and C-red-P1 AGREE timestamps, not implementer narrative.'
            'Treat plugin Test-MarkerSignature as authoritative. Do not require P4-P20, Phase C complete, or PLAN/PLUGININT done.'
        )
        tags = @('hostile-validator','PLAN-PLUGINHANDOFF-001','MCP-PLUGININT-001','C-green-P1-P3','FR-MCP-PLUGININT-001','TR-MCP-PLUGININT-001','TEST-MCP-PLUGININT-001')
        contextList = @('docs/plans/PLAN-PLUGINHANDOFF-001.md','tests/Build.Tests/PluginSessionLogIntegrationTargetTests.cs','docs/receipts/hostile-validator-20260821T232545Z.md','docs/receipts/hostile-validator-20260821T234422Z.md')
        interpretation = 'Validate Phase C-green-P1-P3 claims. Review only. Do not implement. Do not mark TODOs done.'
    }
} -Name 'sl-patch'

Invoke-Plugin -Method 'client.SessionLog.CompleteTurnAsync' -Params @{
    agent = $agent
    sessionId = $sessionId
    requestId = $requestId
    payload = @{
        requestId = $requestId
        status = 'completed'
        response = 'Hostile C-green-P1-P3 review AGREE. Receipt docs/receipts/hostile-validator-20260821T234422Z.md. Build.Tests PluginSessionLogIntegrationTargetTests Passed 3 Failed 0 Skipped 0. PluginIntegration.Tests Passed 1 Failed 0 Skipped 0. PLAN-PLUGINHANDOFF-001 and MCP-PLUGININT-001 remain done false.'
        queryTitle = 'Hostile validate C-green-P1-P3 plugin sessionlog harness'
    }
} -Name 'sl-complete'

Invoke-Plugin -Method 'client.SessionLog.QueryAsync' -Params @{
    agent = $agent
    limit = 10
} -Name 'sl-query-agent'

Invoke-Plugin -Method 'client.SessionLog.QueryAsync' -Params @{
    agent = $agent
    sessionId = $sessionId
    limit = 5
} -Name 'sl-query-sid'

Invoke-Plugin -Method 'client.SessionLog.QueryAsync' -Params @{
    agent = $agent
    todoId = 'PLAN-PLUGINHANDOFF-001'
    limit = 5
} -Name 'sl-query-todo'

Write-Output 'FINISH_SESSION_DONE'
