#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$out = 'F:\GitHub\McpServer\docs\receipts\_hv-c-green-p5-p6'
$plugin = 'F:\GitHub\mcpserver-grok-plugin\lib\Invoke-McpPlugin.ps1'
Set-Location -LiteralPath 'F:\GitHub\McpServer'
$env:MCP_WORKSPACE_PATH = 'F:\GitHub\McpServer'
$env:MCPSERVER_WORKSPACE_PATH = 'F:\GitHub\McpServer'
$env:GROK_WORKSPACE_PATH = 'F:\GitHub\McpServer'
$env:PLUGIN_AGENT_NAME = 'GrokSubagentHostile'
$env:MCP_AGENT_NAME = 'GrokSubagentHostile'
$env:GROK_PLUGIN_ROOT = 'F:\GitHub\mcpserver-grok-plugin'
$env:MCP_PLUGIN_ROOT = 'F:\GitHub\mcpserver-grok-plugin'
$env:MCP_PLUGIN_HOST = 'grok'

$agent = 'GrokSubagentHostile'
$sessionId = 'GrokSubagentHostile-20260822T011806Z-c-green-p5-p6'
$requestId = 'req-20260822T011806Z-001-hostile-c-green-p5-p6'
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

$receiptMd = Get-Item -LiteralPath 'F:\GitHub\McpServer\docs\receipts\hostile-validator-20260822T012355Z.md'
$receiptJson = Get-Item -LiteralPath 'F:\GitHub\McpServer\docs\receipts\hostile-validator-20260822T012355Z.json'
$mdText = Get-Content -LiteralPath $receiptMd.FullName -Raw
$jsonObj = Get-Content -LiteralPath $receiptJson.FullName -Raw | ConvertFrom-Json
$proof = [ordered]@{
    MdExists = $true
    MdLength = $receiptMd.Length
    MdLastWriteTimeUtc = $receiptMd.LastWriteTimeUtc.ToString('o')
    MdHasAgree = [bool]($mdText -match '(?m)^OverallVerdict:\s*AGREE\s*$')
    JsonExists = $true
    JsonLength = $receiptJson.Length
    JsonLastWriteTimeUtc = $receiptJson.LastWriteTimeUtc.ToString('o')
    JsonOverallVerdict = [string]$jsonObj.OverallVerdict
    JsonFailCount = $jsonObj.FailCount
    EmDashInMd = $mdText.Contains([char]0x2014)
    EnDashInMd = $mdText.Contains([char]0x2013)
}
$proof | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $out 'receipt-on-disk.json') -Encoding utf8

$dialogItems = @(
    @{ timestamp = $now; role = 'model'; category = 'observation'; content = 'Independent re-run PluginIntegrationServerFixtureTests Failed 0 Passed 7 Skipped 0 FILTER_EXIT=0 DurationMs=141111. Full PluginIntegration.Tests Passed 14 Failed 0 Skipped 0 ALL_EXIT=0. StartAsync launches dotnet + McpServer.Support.Mcp.dll, not WebApplicationFactory. Adapter_CapturesExecutable NO_MATCH. Live todo_get PLAN-PLUGINHANDOFF-001 done false; MCP-PLUGININT-001 done false P5/P6/P7 done false.' }
    @{ timestamp = $now; role = 'model'; category = 'decision'; content = 'Decision: AGREE C-green-P5-P6. Attacks not sustained: still throws not implemented; uses 7147; uses developer DB; fake marker without process; skipped tests; P7 mixed in; PLAN done. Consequence: C-green-P5-P6 gate may proceed to C-red-P7 only after this AGREE; do not mark PLAN or PLUGININT done; do not require P7-P20. Alternatives rejected: DISAGREE because SignatureAndNonceTrust does not recompute HMAC (residual operational trust via nonce plus QueryAsync); DISAGREE because StartupTimeout accepts either cancel or success (TRX 1.096s is the cancel path); homemade HMAC false as MCP_UNTRUSTED.' }
    @{ timestamp = $now; role = 'model'; category = 'observation'; content = 'C-red-P5 AGREE on disk. Fixture files LastWriteTime after 2026-08-22T00:51:08Z. Plugin signature true. Health nonce aa3c36eb250446bda62d0c8fb6ae9905 echoed. Receipt docs/receipts/hostile-validator-20260822T012355Z.md OverallVerdict AGREE FailCount 0.' }
)

Invoke-Plugin -Method 'client.SessionLog.AppendDialogAsync' -Params @{
    agent = $agent
    sessionId = $sessionId
    requestId = $requestId
    items = $dialogItems
} -Name 'sl-dialog'

$actions = @(
    @{ order = 1; description = 'Executed add-profile; read 18 profile markdown files'; type = 'read'; status = 'completed'; filePath = 'C:\Users\kingd\.claude\profile\PROFILE.md' }
    @{ order = 2; description = 'Health nonce aa3c36eb250446bda62d0c8fb6ae9905 echoed; plugin Test-MarkerSignature true; BeginTurnAsync turnId 42836'; type = 'read'; status = 'completed'; filePath = 'F:\GitHub\McpServer\AGENTS-README-FIRST.yaml' }
    @{ order = 3; description = 'Re-ran PluginIntegrationServerFixtureTests Failed 0 Passed 7 Skipped 0; StartAsync launches compiled Support.Mcp.dll'; type = 'test'; status = 'completed'; filePath = 'tests/McpServer.PluginIntegration.Tests/PluginIntegrationServerFixture.cs' }
    @{ order = 4; description = 'Live todo_get PLAN-PLUGINHANDOFF-001 done false; MCP-PLUGININT-001 done false P5 P6 P7 done false; Adapter_CapturesExecutable absent'; type = 'read'; status = 'completed'; filePath = 'docs/receipts/_hv-c-green-p5-p6/todo-plan.txt' }
    @{ order = 5; description = 'Wrote hostile receipt pair 20260822T012355Z OverallVerdict AGREE FailCount 0'; type = 'create'; status = 'completed'; filePath = 'docs/receipts/hostile-validator-20260822T012355Z.md' }
    @{ order = 6; description = 'AGREE because named P5/P6 tests pass against compiled isolated host, PLAN stays not done, P7 adapter tests absent'; type = 'design_decision'; status = 'completed'; filePath = '' }
)

Invoke-Plugin -Method 'client.SessionLog.PatchTurnAsync' -Params @{
    agent = $agent
    sessionId = $sessionId
    requestId = $requestId
    turn = @{
        requestId = $requestId
        queryTitle = 'Hostile C-green-P5-P6 isolated server fixture greens'
        actions = $actions
        designDecisions = @(
            'Score C-green-P5-P6 from independently re-run PluginIntegrationServerFixtureTests, live MCP store, C-red-P5 AGREE timestamps, and fixture source, not implementer narrative.'
            'AGREE: filter Failed 0 Passed 7 Skipped 0; StartAsync launches compiled Support.Mcp.dll on a free loopback port; isolated workspace/db; marker signature and health nonce; DisposeAsync cleanup; HealthReady QueryAsync; PLAN and PLUGININT remain done false; Adapter_CapturesExecutable absent. Do not mark PLAN/PLUGININT done. Do not require P7-P20.'
        )
        tags = @('hostile-validator','PLAN-PLUGINHANDOFF-001','MCP-PLUGININT-001','C-green-P5-P6','FR-MCP-PLUGININT-001','TR-MCP-PLUGININT-001','TEST-MCP-PLUGININT-001')
        contextList = @('docs/plans/PLAN-PLUGINHANDOFF-001.md','tests/McpServer.PluginIntegration.Tests/PluginIntegrationServerFixtureTests.cs','tests/McpServer.PluginIntegration.Tests/PluginIntegrationServerFixture.cs','docs/receipts/hostile-validator-20260822T005108Z.md','docs/receipts/hostile-validator-20260822T012355Z.md')
        interpretation = 'Hostile review Phase C-green-P5-P6 after C-red-P5 AGREE. Review only. Do not implement. Do not mark TODOs done. Do not require P7-P20.'
        filesModified = @('docs/receipts/hostile-validator-20260822T012355Z.md','docs/receipts/hostile-validator-20260822T012355Z.json')
    }
} -Name 'sl-patch'

Invoke-Plugin -Method 'client.SessionLog.CompleteTurnAsync' -Params @{
    agent = $agent
    sessionId = $sessionId
    requestId = $requestId
    payload = @{
        requestId = $requestId
        status = 'completed'
        response = 'Hostile C-green-P5-P6 review AGREE. Receipt docs/receipts/hostile-validator-20260822T012355Z.md. PluginIntegrationServerFixtureTests Failed 0 Passed 7 Skipped 0. StartAsync launches compiled Support.Mcp.dll. Attacks not sustained. PLAN-PLUGINHANDOFF-001 and MCP-PLUGININT-001 remain done false. Do not require P7-P20.'
        queryTitle = 'Hostile C-green-P5-P6 isolated server fixture greens'
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

Invoke-Plugin -Method 'workflow.sessionlog.queryHistory' -Params @{
    agent = $agent
    limit = 5
    offset = 0
} -Name 'sl-query-history'

$receiptMd2 = Get-Item -LiteralPath $receiptMd.FullName
$receiptJson2 = Get-Item -LiteralPath $receiptJson.FullName
[ordered]@{
    AfterQueryMdExists = $true
    AfterQueryMdLength = $receiptMd2.Length
    AfterQueryJsonExists = $true
    AfterQueryJsonLength = $receiptJson2.Length
    AfterQueryJsonVerdict = [string]((Get-Content -LiteralPath $receiptJson2.FullName -Raw | ConvertFrom-Json).OverallVerdict)
} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $out 'receipt-on-disk-after-query.json') -Encoding utf8

Write-Output 'FINISH_SESSION_DONE'
