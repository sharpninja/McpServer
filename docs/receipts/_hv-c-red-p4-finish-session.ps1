#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$out = 'F:\GitHub\McpServer\docs\receipts\_hv-c-red-p4'
$plugin = 'F:\GitHub\mcpserver-grok-plugin\lib\Invoke-McpPlugin.ps1'
Set-Location -LiteralPath 'F:\GitHub\McpServer'
$env:MCP_WORKSPACE_PATH = 'F:\GitHub\McpServer'
$env:PLUGIN_AGENT_NAME = 'GrokSubagentHostile'
$env:MCP_AGENT_NAME = 'GrokSubagentHostile'
$env:GROK_PLUGIN_ROOT = 'F:\GitHub\mcpserver-grok-plugin'
$env:MCP_PLUGIN_ROOT = 'F:\GitHub\mcpserver-grok-plugin'

$agent = 'GrokSubagentHostile'
$sessionId = 'GrokSubagentHostile-20260821T235600Z-c-red-p4'
$requestId = 'req-20260821T235600Z-001-hostile-c-red-p4'
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

$receiptMd = Get-Item -LiteralPath 'F:\GitHub\McpServer\docs\receipts\hostile-validator-20260821T235922Z.md'
$receiptJson = Get-Item -LiteralPath 'F:\GitHub\McpServer\docs\receipts\hostile-validator-20260821T235922Z.json'
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
    @{ timestamp = $now; role = 'model'; category = 'observation'; content = 'add-profile executed. 18 non-skill profile files read. Work class 1. Surfaces A+B+C+D apply. Independent re-run PluginSessionLogCatalogTests Failed 6 Passed 0 Skipped 0. LoadAndValidate still throws not implemented. Live todo_get PLAN-PLUGINHANDOFF-001 done false; MCP-PLUGININT-001 done false.' }
    @{ timestamp = $now; role = 'model'; category = 'decision'; content = 'Decision: DISAGREE C-red-P4 despite currently failing named tests. C3 FAIL: TR-MCP-PLUGININT-001 AC1 catalog fields (entrypoint, environment variables, cache folder name) are not pinned; UniqueAgentSourceCache keys AgentSourceType:RepositoryName. D5 FAIL: sibling plugin probe shows a trivial JSON loader would green all six tests. Consequence: do not start P4 green; do not mark PLAN or PLUGININT done. Alternatives rejected: AGREE on named-test existence alone; treating P2 green in the same project as mixed P4 green; homemade HMAC false as MCP_UNTRUSTED.' }
    @{ timestamp = $now; role = 'model'; category = 'observation'; content = 'C-green-P1-P3 AGREE on disk. P4 test files after 23:48:25Z. Plugin signature true. Health nonce 49d2da7c853647a3b72d361aa98bba42 echoed. Receipt docs/receipts/hostile-validator-20260821T235922Z.md OverallVerdict DISAGREE FailCount 2.' }
)

Invoke-Plugin -Method 'client.SessionLog.AppendDialogAsync' -Params @{
    agent = $agent
    sessionId = $sessionId
    requestId = $requestId
    items = $dialogItems
} -Name 'sl-dialog'

$actions = @(
    @{ order = 1; description = 'Executed add-profile; read 18 profile markdown files'; type = 'read'; status = 'completed'; filePath = 'C:\Users\kingd\.claude\profile\PROFILE.md' }
    @{ order = 2; description = 'Health nonce 49d2da7c853647a3b72d361aa98bba42 echoed; plugin Test-MarkerSignature true; BeginTurnAsync turnId 42807'; type = 'read'; status = 'completed'; filePath = 'F:\GitHub\McpServer\AGENTS-README-FIRST.yaml' }
    @{ order = 3; description = 'Re-ran PluginSessionLogCatalogTests Failed 6 Passed 0 Skipped 0; LoadAndValidate not implemented'; type = 'test'; status = 'completed'; filePath = 'tests/McpServer.PluginIntegration.Tests/PluginSessionLogCatalogTests.cs' }
    @{ order = 4; description = 'Live todo_get PLAN-PLUGINHANDOFF-001 done false; MCP-PLUGININT-001 done false P4 task done false'; type = 'read'; status = 'completed'; filePath = 'docs/receipts/_hv-c-red-p4/todo-plan-pluginhandoff-001.txt' }
    @{ order = 5; description = 'Wrote hostile receipt pair 20260821T235922Z OverallVerdict DISAGREE FailCount 2'; type = 'create'; status = 'completed'; filePath = 'docs/receipts/hostile-validator-20260821T235922Z.md' }
    @{ order = 6; description = 'DISAGREE because P4 reds do not pin TR AC1 cache/entrypoint/env; trivial loader would go green'; type = 'design_decision'; status = 'completed'; filePath = '' }
)

Invoke-Plugin -Method 'client.SessionLog.PatchTurnAsync' -Params @{
    agent = $agent
    sessionId = $sessionId
    requestId = $requestId
    turn = @{
        requestId = $requestId
        queryTitle = 'Hostile validate C-red-P4 plugin sessionlog catalog tests'
        actions = $actions
        designDecisions = @(
            'Score C-red-P4 from independently re-run CatalogTests, live MCP store, C-green-P1-P3 AGREE timestamps, and sibling plugin probe, not implementer narrative.'
            'DISAGREE: named tests exist and fail, but they do not cover TR-MCP-PLUGININT-001 AC1 catalog fields. Do not start P4 green. Do not mark PLAN/PLUGININT done.'
        )
        tags = @('hostile-validator','PLAN-PLUGINHANDOFF-001','MCP-PLUGININT-001','C-red-P4','FR-MCP-PLUGININT-001','TR-MCP-PLUGININT-001','TEST-MCP-PLUGININT-001')
        contextList = @('docs/plans/PLAN-PLUGINHANDOFF-001.md','tests/McpServer.PluginIntegration.Tests/PluginSessionLogCatalogTests.cs','tests/McpServer.PluginIntegration.Tests/PluginSessionLogCatalog.cs','docs/receipts/hostile-validator-20260821T234422Z.md','docs/receipts/hostile-validator-20260821T235922Z.md')
        interpretation = 'Validate Phase C-red-P4 claims. Review only. Do not implement. Do not mark TODOs done.'
    }
} -Name 'sl-patch'

Invoke-Plugin -Method 'client.SessionLog.CompleteTurnAsync' -Params @{
    agent = $agent
    sessionId = $sessionId
    requestId = $requestId
    payload = @{
        requestId = $requestId
        status = 'completed'
        response = 'Hostile C-red-P4 review DISAGREE. Receipt docs/receipts/hostile-validator-20260821T235922Z.md. PluginSessionLogCatalogTests Failed 6 Passed 0 Skipped 0. LoadAndValidate not implemented. FAIL C3 TR AC1 catalog fields not pinned; FAIL D5 trivial loader would green. PLAN-PLUGINHANDOFF-001 and MCP-PLUGININT-001 remain done false.'
        queryTitle = 'Hostile validate C-red-P4 plugin sessionlog catalog tests'
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
