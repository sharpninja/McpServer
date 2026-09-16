#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$out = 'F:\GitHub\McpServer\docs\receipts\_hv-c-red-p4-r3'
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
$sessionId = 'GrokSubagentHostile-20260822T002701Z-c-red-p4-r3'
$requestId = 'req-20260822T002701Z-001-hostile-c-red-p4-r3'
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

$receiptMd = Get-Item -LiteralPath 'F:\GitHub\McpServer\docs\receipts\hostile-validator-20260822T003120Z.md'
$receiptJson = Get-Item -LiteralPath 'F:\GitHub\McpServer\docs\receipts\hostile-validator-20260822T003120Z.json'
$mdText = Get-Content -LiteralPath $receiptMd.FullName -Raw
$jsonObj = Get-Content -LiteralPath $receiptJson.FullName -Raw | ConvertFrom-Json
$proof = [ordered]@{
    MdExists = $true
    MdLength = $receiptMd.Length
    MdLastWriteTimeUtc = $receiptMd.LastWriteTimeUtc.ToString('o')
    MdHasDisagree = [bool]($mdText -match '(?m)^OverallVerdict:\s*DISAGREE\s*$')
    JsonExists = $true
    JsonLength = $receiptJson.Length
    JsonLastWriteTimeUtc = $receiptJson.LastWriteTimeUtc.ToString('o')
    JsonOverallVerdict = [string]$jsonObj.OverallVerdict
    JsonFailCount = $jsonObj.FailCount
}
$proof | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $out 'receipt-on-disk.json') -Encoding utf8

$dialogItems = @(
    @{ timestamp = $now; role = 'model'; category = 'observation'; content = 'Independent re-run PluginSessionLogCatalogTests Passed 6 Failed 0 Skipped 0. Whole PluginIntegration.Tests Passed 7 Failed 0 Skipped 0. LoadAndValidate is implemented (deserialize plus AC1/root/entrypoint/version/eight/unique-key checks), not a not-implemented throw. Live todo_get PLAN-PLUGINHANDOFF-001 done false; MCP-PLUGININT-001 done false P4 done false. Live JSON Codex entrypoint is Invoke-CodexMcpPlugin.ps1 and the file exists. deserialize-simulate AllSixWouldPass true.' }
    @{ timestamp = $now; role = 'model'; category = 'decision'; content = 'Decision: DISAGREE this C-red-P4 re-gate. Parent claimed tests still fail via not-implemented throw and that P4 green has not started. Artifacts show P4 green on disk after r2 AGREE. Consequence: cannot AGREE C-red-P4; cannot mark PLAN or PLUGININT or P4 done; do not treat this as C-green-P4 either because the brief mixed a red-gate with green artifacts and false throw-only claims. Alternatives rejected: AGREE because C3 coverage is now present (coverage is not a red-gate while tests are green); treat deserialize-only AllSixWouldPass as a hold that still allows C-red AGREE (parent instruction forbids that).' }
    @{ timestamp = $now; role = 'model'; category = 'observation'; content = 'C-green-P1-P3 AGREE on disk. Prior DISAGREE 235922Z C3/D5. r2 AGREE 001811Z then Catalog.cs 00:23:02Z implementation. Plugin signature true. Health nonce 77ee702554f947ddb119539070731850 echoed. Receipt docs/receipts/hostile-validator-20260822T003120Z.md OverallVerdict DISAGREE FailCount 6.' }
)

Invoke-Plugin -Method 'client.SessionLog.AppendDialogAsync' -Params @{
    agent = $agent
    sessionId = $sessionId
    requestId = $requestId
    items = $dialogItems
} -Name 'sl-dialog'

$actions = @(
    @{ order = 1; description = 'Executed add-profile; read 18 profile markdown files'; type = 'read'; status = 'completed'; filePath = 'C:\Users\kingd\.claude\profile\PROFILE.md' }
    @{ order = 2; description = 'Health nonce 77ee702554f947ddb119539070731850 echoed; plugin Test-MarkerSignature true; BeginTurnAsync turnId 42820'; type = 'read'; status = 'completed'; filePath = 'F:\GitHub\McpServer\AGENTS-README-FIRST.yaml' }
    @{ order = 3; description = 'Re-ran PluginSessionLogCatalogTests Passed 6 Failed 0 Skipped 0; LoadAndValidate implemented'; type = 'test'; status = 'completed'; filePath = 'tests/McpServer.PluginIntegration.Tests/PluginSessionLogCatalogTests.cs' }
    @{ order = 4; description = 'Live todo_get PLAN-PLUGINHANDOFF-001 done false; MCP-PLUGININT-001 done false P4 task done false'; type = 'read'; status = 'completed'; filePath = 'docs/receipts/_hv-c-red-p4-r3/todo-plan-pluginhandoff-001.txt' }
    @{ order = 5; description = 'Wrote hostile receipt pair 20260822T003120Z OverallVerdict DISAGREE FailCount 6'; type = 'create'; status = 'completed'; filePath = 'docs/receipts/hostile-validator-20260822T003120Z.md' }
    @{ order = 6; description = 'DISAGREE because C-red-P4 brief mixed P4 green artifacts and a deserialize-only loader would pass all six tests'; type = 'design_decision'; status = 'completed'; filePath = '' }
)

Invoke-Plugin -Method 'client.SessionLog.PatchTurnAsync' -Params @{
    agent = $agent
    sessionId = $sessionId
    requestId = $requestId
    turn = @{
        requestId = $requestId
        queryTitle = 'Hostile C-red-P4 r3 re-gate catalog AC1 and TDD hold'
        actions = $actions
        designDecisions = @(
            'Score C-red-P4 r3 from independently re-run CatalogTests, live MCP store, r2 AGREE timestamps, implemented LoadAndValidate, and live-JSON deserialize simulation, not implementer narrative.'
            'DISAGREE: P4 green is on disk; tests Passed 6; naive deserialize of live JSON would pass all six. Do not mark PLAN/PLUGININT/P4 done. Do not AGREE this red-gate. Do not treat this receipt as C-green-P4.'
        )
        tags = @('hostile-validator','PLAN-PLUGINHANDOFF-001','MCP-PLUGININT-001','C-red-P4','FR-MCP-PLUGININT-001','TR-MCP-PLUGININT-001','TEST-MCP-PLUGININT-001')
        contextList = @('docs/plans/PLAN-PLUGINHANDOFF-001.md','tests/McpServer.PluginIntegration.Tests/PluginSessionLogCatalogTests.cs','tests/McpServer.PluginIntegration.Tests/PluginSessionLogCatalog.cs','tests/McpServer.PluginIntegration.Tests/PluginSessionLogScenario.cs','tests/McpServer.PluginIntegration.Tests/scenarios/plugin-sessionlog-scenarios.json','docs/receipts/hostile-validator-20260821T235922Z.md','docs/receipts/hostile-validator-20260822T001811Z.md','docs/receipts/hostile-validator-20260822T003120Z.md')
        interpretation = 'Re-gate Phase C-red-P4 after DISAGREE. Review only. Do not implement. Do not mark TODOs done.'
        filesModified = @('docs/receipts/hostile-validator-20260822T003120Z.md','docs/receipts/hostile-validator-20260822T003120Z.json')
        requirementsDiscovered = @('FR-MCP-PLUGININT-001','TR-MCP-PLUGININT-001','TEST-MCP-PLUGININT-001')
        planFile = 'docs/plans/PLAN-PLUGINHANDOFF-001.md'
        todoId = 'PLAN-PLUGINHANDOFF-001'
    }
} -Name 'sl-patch'

$completeResponse = 'Hostile C-red-P4 r3 DISAGREE. Receipt docs/receipts/hostile-validator-20260822T003120Z.md. PluginSessionLogCatalogTests Passed 6 Failed 0 Skipped 0. LoadAndValidate implemented. Naive deserialize of live JSON AllSixWouldPass true. PLAN-PLUGINHANDOFF-001 and MCP-PLUGININT-001 remain done false. Do not claim C-red-P4 AGREE. Do not mark P4 done.'

Invoke-Plugin -Method 'client.SessionLog.CompleteTurnAsync' -Params @{
    agent = $agent
    sessionId = $sessionId
    requestId = $requestId
    payload = @{
        requestId = $requestId
        status = 'completed'
        response = $completeResponse
        queryTitle = 'Hostile C-red-P4 r3 re-gate catalog AC1 and TDD hold'
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

Write-Output 'FINISH_SESSION_DONE'
