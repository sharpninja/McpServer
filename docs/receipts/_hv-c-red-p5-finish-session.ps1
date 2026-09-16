#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$out = 'F:\GitHub\McpServer\docs\receipts\_hv-c-red-p5'
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
$sessionId = 'GrokSubagentHostile-20260822T004733Z-c-red-p5'
$requestId = 'req-20260822T004733Z-001-hostile-c-red-p5-fixture'
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

$receiptMd = Get-Item -LiteralPath 'F:\GitHub\McpServer\docs\receipts\hostile-validator-20260822T005108Z.md'
$receiptJson = Get-Item -LiteralPath 'F:\GitHub\McpServer\docs\receipts\hostile-validator-20260822T005108Z.json'
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
}
$proof | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $out 'receipt-on-disk.json') -Encoding utf8

$dialogItems = @(
    @{ timestamp = $now; role = 'model'; category = 'observation'; content = 'Independent re-run PluginIntegrationServerFixtureTests Failed 6 Passed 0 Skipped 0. Full PluginIntegration.Tests Passed 7 Failed 6 Skipped 0. StartAsync throws PluginIntegrationServerFixture.StartAsync is not implemented. No WebApplication/Kestrel. No Port=7147. Live todo_get PLAN-PLUGINHANDOFF-001 done false; MCP-PLUGININT-001 done false P5/P6 done false.' }
    @{ timestamp = $now; role = 'model'; category = 'decision'; content = 'Decision: AGREE C-red-P5. Attacks not sustained: fixture does not start a real host; tests are not skipped; P5 greens are not mixed in; 7147 is not reused; PLAN is not done. Consequence: C-red-P5 gate may proceed to P6 green only after this AGREE; do not mark PLAN or PLUGININT done; do not require P5 green. Alternatives rejected: DISAGREE because StartupTimeout can pass if StartAsync later ignores CTS (residual for P6); DISAGREE because class name is PluginIntegrationServerFixture not PluginSessionLogServerFixture (plan names the six methods, which match); homemade HMAC false as MCP_UNTRUSTED.' }
    @{ timestamp = $now; role = 'model'; category = 'observation'; content = 'C-green-P4 AGREE on disk. Fixture files LastWriteTime after 2026-08-22T00:38:17Z and untracked. Plugin signature true. Health nonce 22f296d079bc456491d751e4e67d0861 echoed. Receipt docs/receipts/hostile-validator-20260822T005108Z.md OverallVerdict AGREE FailCount 0.' }
)

Invoke-Plugin -Method 'client.SessionLog.AppendDialogAsync' -Params @{
    agent = $agent
    sessionId = $sessionId
    requestId = $requestId
    items = $dialogItems
} -Name 'sl-dialog'

$actions = @(
    @{ order = 1; description = 'Executed add-profile; read 18 profile markdown files'; type = 'read'; status = 'completed'; filePath = 'C:\Users\kingd\.claude\profile\PROFILE.md' }
    @{ order = 2; description = 'Health nonce 22f296d079bc456491d751e4e67d0861 echoed; plugin Test-MarkerSignature true; BeginTurnAsync turnId 42827'; type = 'read'; status = 'completed'; filePath = 'F:\GitHub\McpServer\AGENTS-README-FIRST.yaml' }
    @{ order = 3; description = 'Re-ran PluginIntegrationServerFixtureTests Failed 6 Passed 0 Skipped 0; StartAsync throws not implemented'; type = 'test'; status = 'completed'; filePath = 'tests/McpServer.PluginIntegration.Tests/PluginIntegrationServerFixture.cs' }
    @{ order = 4; description = 'Live todo_get PLAN-PLUGINHANDOFF-001 done false; MCP-PLUGININT-001 done false P5 and P6 done false'; type = 'read'; status = 'completed'; filePath = 'docs/receipts/_hv-c-red-p5/todo-plan.txt' }
    @{ order = 5; description = 'Wrote hostile receipt pair 20260822T005108Z OverallVerdict AGREE FailCount 0'; type = 'create'; status = 'completed'; filePath = 'docs/receipts/hostile-validator-20260822T005108Z.md' }
    @{ order = 6; description = 'AGREE because six named P5 tests exist, fail, are new vs P4 greens, stub does not use 7147, PLAN stays not done'; type = 'design_decision'; status = 'completed'; filePath = '' }
)

Invoke-Plugin -Method 'client.SessionLog.PatchTurnAsync' -Params @{
    agent = $agent
    sessionId = $sessionId
    requestId = $requestId
    turn = @{
        requestId = $requestId
        queryTitle = 'Hostile C-red-P5 isolated server fixture reds'
        actions = $actions
        designDecisions = @(
            'Score C-red-P5 from independently re-run PluginIntegrationServerFixtureTests, live MCP store, C-green-P4 AGREE timestamps, and fixture source, not implementer narrative.'
            'AGREE: six named P5 tests exist and fail Failed 6 Skipped 0; StartAsync throws not implemented; files are new after C-green-P4 AGREE; PLAN and PLUGININT remain done false. Do not mark PLAN/PLUGININT done. Do not require P5 green.'
        )
        tags = @('hostile-validator','PLAN-PLUGINHANDOFF-001','MCP-PLUGININT-001','C-red-P5','FR-MCP-PLUGININT-001','TR-MCP-PLUGININT-001','TEST-MCP-PLUGININT-001')
        contextList = @('docs/plans/PLAN-PLUGINHANDOFF-001.md','tests/McpServer.PluginIntegration.Tests/PluginIntegrationServerFixtureTests.cs','tests/McpServer.PluginIntegration.Tests/PluginIntegrationServerFixture.cs','docs/receipts/hostile-validator-20260822T003817Z.md','docs/receipts/hostile-validator-20260822T005108Z.md')
        interpretation = 'Hostile review Phase C-red-P5 after C-green-P4 AGREE. Review only. Do not implement. Do not mark TODOs done. Do not require P5 green.'
    }
} -Name 'sl-patch'

Invoke-Plugin -Method 'client.SessionLog.CompleteTurnAsync' -Params @{
    agent = $agent
    sessionId = $sessionId
    requestId = $requestId
    payload = @{
        requestId = $requestId
        status = 'completed'
        response = 'Hostile C-red-P5 review AGREE. Receipt docs/receipts/hostile-validator-20260822T005108Z.md. PluginIntegrationServerFixtureTests Failed 6 Passed 0 Skipped 0. StartAsync throws not implemented. Attacks not sustained. PLAN-PLUGINHANDOFF-001 and MCP-PLUGININT-001 remain done false. Do not require P5 green.'
        queryTitle = 'Hostile C-red-P5 isolated server fixture reds'
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
