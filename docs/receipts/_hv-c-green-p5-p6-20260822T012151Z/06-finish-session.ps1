#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$out = 'F:\GitHub\McpServer\docs\receipts\_hv-c-green-p5-p6-20260822T012151Z'
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
$sessionId = 'GrokSubagentHostile-20260822T012646Z-c-green-p5-p6'
$requestId = 'req-20260822T012646Z-001-hostile-c-green-p5-p6'
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

$receiptMd = Get-Item -LiteralPath 'F:\GitHub\McpServer\docs\receipts\hostile-validator-20260822T013544Z.md'
$receiptJson = Get-Item -LiteralPath 'F:\GitHub\McpServer\docs\receipts\hostile-validator-20260822T013544Z.json'
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
    @{ timestamp = $now; role = 'model'; category = 'observation'; content = 'Independent re-run PluginIntegrationServerFixtureTests Passed 7 Failed 0 Skipped 0. Full PluginIntegration.Tests Passed 14 Failed 0 Skipped 0. StartAsync launches compiled Support.Mcp via Process. Temp leftover mcp-pluginint count 0. Developer 7147 still pid 16936. PLAN and MCP-PLUGININT done false. P7 Adapter_Captures absent.' }
    @{ timestamp = $now; role = 'model'; category = 'decision'; content = 'Decision: AGREE C-green-P5-P6. Attacks not sustained: StartAsync is no longer a throw stub; tests are not skipped; Port is never assigned 7147 (ReservedServicePort is exclusion only); developer sqlite was not the fixture DataSource; P7 is not in this receipt; PLAN is not done. Consequence: C-green-P5-P6 gate may proceed to C-red-P7; do not mark PLAN or PLUGININT done. Alternatives rejected: DISAGREE because parent collector FixtureHardcodes7147PortAssignment true (regex false positive on ReservedServicePort); DISAGREE because Production UseUrls binds all interfaces (residual, not AC2 fail); DISAGREE because P1-P4 tasks still false (separate goal-state work).' }
    @{ timestamp = $now; role = 'model'; category = 'observation'; content = 'C-red-P5 AGREE on disk 20260822T005108Z. Plugin signature true. Health nonce 42804f263bf84a25a9f1523539a4b563 echoed. Receipt docs/receipts/hostile-validator-20260822T013544Z.md OverallVerdict AGREE FailCount 0.' }
)

Invoke-Plugin -Method 'client.SessionLog.AppendDialogAsync' -Params @{
    agent = $agent
    sessionId = $sessionId
    requestId = $requestId
    items = $dialogItems
} -Name 'sl-dialog'

$actions = @(
    @{ order = 1; description = 'Executed add-profile; read 18 profile markdown files'; type = 'read'; status = 'completed'; filePath = 'C:\Users\kingd\.claude\profile\PROFILE.md' }
    @{ order = 2; description = 'Health nonce 42804f263bf84a25a9f1523539a4b563 echoed; plugin Test-MarkerSignature true; BeginTurnAsync turnId 42839'; type = 'read'; status = 'completed'; filePath = 'F:\GitHub\McpServer\AGENTS-README-FIRST.yaml' }
    @{ order = 3; description = 'Re-ran PluginIntegrationServerFixtureTests Passed 7 Failed 0 Skipped 0 and full project Passed 14 Failed 0 Skipped 0'; type = 'test'; status = 'completed'; filePath = 'tests/McpServer.PluginIntegration.Tests/PluginIntegrationServerFixtureTests.cs' }
    @{ order = 4; description = 'Live todo_get PLAN-PLUGINHANDOFF-001 done false; MCP-PLUGININT-001 done false P5 P6 P7 done false'; type = 'read'; status = 'completed'; filePath = 'docs/receipts/_hv-c-green-p5-p6-20260822T012151Z/todo-pluginint.txt' }
    @{ order = 5; description = 'Wrote hostile receipt pair 20260822T013544Z OverallVerdict AGREE FailCount 0'; type = 'create'; status = 'completed'; filePath = 'docs/receipts/hostile-validator-20260822T013544Z.md' }
    @{ order = 6; description = 'AGREE because P5/P6 fixture tests pass after C-red-P5 AGREE; isolated from developer 7147 DB; PLAN stays not done; not P7'; type = 'design_decision'; status = 'completed'; filePath = '' }
)

Invoke-Plugin -Method 'client.SessionLog.PatchTurnAsync' -Params @{
    agent = $agent
    sessionId = $sessionId
    requestId = $requestId
    turn = @{
        requestId = $requestId
        queryTitle = 'Hostile validate C-green-P5-P6 isolated server fixture'
        actions = $actions
        designDecisions = @(
            'Score C-green-P5-P6 from independently re-run PluginIntegration.Tests, live MCP store, C-red-P5 AGREE timestamps, developer pid 16936, and temp leftover count, not implementer narrative.'
            'AGREE: StartAsync launches compiled Support.Mcp on an isolated temp workspace; seven fixture tests plus prior catalog tests are Failed 0 Skipped 0. Do not mark PLAN/PLUGININT done. Do not treat this as P7.'
        )
        tags = @('hostile-validator','PLAN-PLUGINHANDOFF-001','MCP-PLUGININT-001','C-green-P5-P6','FR-MCP-PLUGININT-001','TR-MCP-PLUGININT-001','TEST-MCP-PLUGININT-001')
        contextList = @('docs/plans/PLAN-PLUGINHANDOFF-001.md','tests/McpServer.PluginIntegration.Tests/PluginIntegrationServerFixture.cs','tests/McpServer.PluginIntegration.Tests/PluginIntegrationServerFixtureTests.cs','docs/receipts/hostile-validator-20260822T005108Z.md','docs/receipts/hostile-validator-20260822T013544Z.md')
        interpretation = 'Hostile review Phase C-green-P5-P6 after C-red-P5 AGREE. Review only. Do not implement P7. Do not mark TODOs done.'
    }
} -Name 'sl-patch'

Invoke-Plugin -Method 'client.SessionLog.CompleteTurnAsync' -Params @{
    agent = $agent
    sessionId = $sessionId
    requestId = $requestId
    payload = @{
        requestId = $requestId
        status = 'completed'
        response = 'Hostile C-green-P5-P6 review AGREE. Receipt docs/receipts/hostile-validator-20260822T013544Z.md. PluginIntegrationServerFixtureTests Passed 7 Failed 0 Skipped 0. Full PluginIntegration.Tests Passed 14 Failed 0 Skipped 0. Isolated from developer 7147 DB. Attacks not sustained. PLAN-PLUGINHANDOFF-001 and MCP-PLUGININT-001 remain done false. Not P7.'
        queryTitle = 'Hostile validate C-green-P5-P6 isolated server fixture'
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
