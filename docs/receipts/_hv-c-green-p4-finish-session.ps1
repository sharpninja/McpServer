#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$out = 'F:\GitHub\McpServer\docs\receipts\_hv-c-green-p4'
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
$sessionId = 'GrokSubagentHostile-20260822T003138Z-c-green-p4'
$requestId = 'req-20260822T003138Z-001-hostile-c-green-p4-catalog'
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

$receiptMd = Get-Item -LiteralPath 'F:\GitHub\McpServer\docs\receipts\hostile-validator-20260822T003817Z.md'
$receiptJson = Get-Item -LiteralPath 'F:\GitHub\McpServer\docs\receipts\hostile-validator-20260822T003817Z.json'
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
    @{ timestamp = $now; role = 'model'; category = 'observation'; content = 'Independent re-run PluginIntegration.Tests Passed 7 Failed 0 Skipped 0. LoadAndValidate implemented: reads plugin-sessionlog-scenarios.json and maps CacheFolder, Entrypoint, RequiredEnvironmentVariables, agent source type, host kind, repository. Codex entrypoint Invoke-CodexMcpPlugin.ps1 at plugin root, not lib/. Cline v2 cacheFolder cline-v2. Uniqueness AgentSourceType:CacheFolder. Fail-closed throws present in source. Live todo_get PLAN-PLUGINHANDOFF-001 done false; MCP-PLUGININT-001 done false P4 done false.' }
    @{ timestamp = $now; role = 'model'; category = 'decision'; content = 'Decision: AGREE C-green-P4. Attacks not sustained: not implemented throw is gone; tests are not skipped; TR AC1 catalog fields are mapped; uniqueness is not repository name; PLAN is not done. Consequence: C-green-P4 gate may proceed to C-red-P5; do not mark PLAN or PLUGININT done; do not require P5-P20. Alternatives rejected: DISAGREE because validator fail-closed harness CS0017 (instrumentation, not product); DISAGREE because P1-P3 tasks still false (separate goal-state work); homemade HMAC false as MCP_UNTRUSTED.' }
    @{ timestamp = $now; role = 'model'; category = 'observation'; content = 'C-red-P4 AGREE on disk. Catalog.cs and catalog JSON LastWriteTime after 2026-08-22T00:18:11Z. Plugin signature true. Health nonce 11b1ade28d1c4deabde11943538af051 echoed. Receipt docs/receipts/hostile-validator-20260822T003817Z.md OverallVerdict AGREE FailCount 0.' }
)

Invoke-Plugin -Method 'client.SessionLog.AppendDialogAsync' -Params @{
    agent = $agent
    sessionId = $sessionId
    requestId = $requestId
    items = $dialogItems
} -Name 'sl-dialog'

$actions = @(
    @{ order = 1; description = 'Executed add-profile; read 18 profile markdown files'; type = 'read'; status = 'completed'; filePath = 'C:\Users\kingd\.claude\profile\PROFILE.md' }
    @{ order = 2; description = 'Health nonce 11b1ade28d1c4deabde11943538af051 echoed; plugin Test-MarkerSignature true; BeginTurnAsync turnId 42821'; type = 'read'; status = 'completed'; filePath = 'F:\GitHub\McpServer\AGENTS-README-FIRST.yaml' }
    @{ order = 3; description = 'Re-ran PluginIntegration.Tests Passed 7 Failed 0 Skipped 0; LoadAndValidate implemented'; type = 'test'; status = 'completed'; filePath = 'tests/McpServer.PluginIntegration.Tests/PluginSessionLogCatalog.cs' }
    @{ order = 4; description = 'Live todo_get PLAN-PLUGINHANDOFF-001 done false; MCP-PLUGININT-001 done false P4 task done false'; type = 'read'; status = 'completed'; filePath = 'docs/receipts/_hv-c-green-p4/todo-plan-pluginhandoff-001.txt' }
    @{ order = 5; description = 'Wrote hostile receipt pair 20260822T003817Z OverallVerdict AGREE FailCount 0'; type = 'create'; status = 'completed'; filePath = 'docs/receipts/hostile-validator-20260822T003817Z.md' }
    @{ order = 6; description = 'AGREE because P4 green tests pass after C-red-P4 AGREE; PLAN stays not done'; type = 'design_decision'; status = 'completed'; filePath = '' }
)

Invoke-Plugin -Method 'client.SessionLog.PatchTurnAsync' -Params @{
    agent = $agent
    sessionId = $sessionId
    requestId = $requestId
    turn = @{
        requestId = $requestId
        queryTitle = 'Hostile C-green-P4 catalog LoadAndValidate'
        actions = $actions
        designDecisions = @(
            'Score C-green-P4 from independently re-run PluginIntegration.Tests, live MCP store, C-red-P4 AGREE timestamps, and catalog-path sibling probe, not implementer narrative.'
            'AGREE: LoadAndValidate maps TR-MCP-PLUGININT-001 AC1 catalog fields and the six P4 tests plus xml-docs test are Passed 7 Failed 0 Skipped 0. Do not mark PLAN/PLUGININT done. Do not require P5-P20.'
        )
        tags = @('hostile-validator','PLAN-PLUGINHANDOFF-001','MCP-PLUGININT-001','C-green-P4','FR-MCP-PLUGININT-001','TR-MCP-PLUGININT-001','TEST-MCP-PLUGININT-001')
        contextList = @('docs/plans/PLAN-PLUGINHANDOFF-001.md','tests/McpServer.PluginIntegration.Tests/PluginSessionLogCatalogTests.cs','tests/McpServer.PluginIntegration.Tests/PluginSessionLogCatalog.cs','tests/McpServer.PluginIntegration.Tests/PluginSessionLogScenario.cs','tests/McpServer.PluginIntegration.Tests/scenarios/plugin-sessionlog-scenarios.json','docs/receipts/hostile-validator-20260822T001811Z.md','docs/receipts/hostile-validator-20260822T003817Z.md')
        interpretation = 'Hostile review Phase C-green-P4 after C-red-P4 AGREE. Review only. Do not implement. Do not mark TODOs done.'
    }
} -Name 'sl-patch'

Invoke-Plugin -Method 'client.SessionLog.CompleteTurnAsync' -Params @{
    agent = $agent
    sessionId = $sessionId
    requestId = $requestId
    payload = @{
        requestId = $requestId
        status = 'completed'
        response = 'Hostile C-green-P4 review AGREE. Receipt docs/receipts/hostile-validator-20260822T003817Z.md. PluginIntegration.Tests Passed 7 Failed 0 Skipped 0. LoadAndValidate implemented. Attacks not sustained. PLAN-PLUGINHANDOFF-001 and MCP-PLUGININT-001 remain done false. Do not require P5-P20.'
        queryTitle = 'Hostile C-green-P4 catalog LoadAndValidate'
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
