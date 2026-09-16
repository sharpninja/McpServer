#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$out = 'F:\GitHub\McpServer\docs\receipts\_hv-c-red-p4-r2'
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
$sessionId = 'GrokSubagentHostile-20260822T001410Z-c-red-p4-r2'
$requestId = 'req-20260822T001410Z-001-hostile-c-red-p4-rereview'
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

$receiptMd = Get-Item -LiteralPath 'F:\GitHub\McpServer\docs\receipts\hostile-validator-20260822T001811Z.md'
$receiptJson = Get-Item -LiteralPath 'F:\GitHub\McpServer\docs\receipts\hostile-validator-20260822T001811Z.json'
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
    @{ timestamp = $now; role = 'model'; category = 'observation'; content = 'Independent re-run PluginSessionLogCatalogTests Failed 6 Passed 0 Skipped 0. LoadAndValidate still throws not implemented. Live todo_get PLAN-PLUGINHANDOFF-001 done false; MCP-PLUGININT-001 done false P4 done false. Scenario type now has CacheFolder, Entrypoint, RequiredEnvironmentVariables. JSON has those fields for eight rows; Cline v2 cacheFolder cline-v2. UniqueAgentSourceCache keys AgentSourceType:CacheFolder. RequiredEntrypointFilesExist uses catalog Entrypoint (no three-path OR) and non-empty env vars. Catalog_ExactlyEightEnabled asserts Cline v2.' }
    @{ timestamp = $now; role = 'model'; category = 'decision'; content = 'Decision: AGREE C-red-P4 after remediations. Prior C3 FAIL is closed: TR-MCP-PLUGININT-001 AC1 catalog fields are typed, in JSON, and pinned by the six red tests. Prior D5 FAIL is closed: trivial JSON deserialize would not green all six (Codex catalog path lib/Invoke-CodexMcpPlugin.ps1 missing; actual file is repo-root Invoke-CodexMcpPlugin.ps1), and the tests that would pass now exercise AC1 fields. Consequence: C-red-P4 gate may proceed to P4 green; do not mark PLAN or PLUGININT done; do not claim P4 green. Alternatives rejected: keep DISAGREE because Codex path is wrong (that is P4 green data, not a red-gate coverage hole); AGREE P4 green; homemade HMAC false as MCP_UNTRUSTED.' }
    @{ timestamp = $now; role = 'model'; category = 'observation'; content = 'C-green-P1-P3 AGREE on disk. Remediation files after prior DISAGREE 23:59:22Z. Plugin signature true. Health nonce f803eb09c53a496fafada1be18070c48 echoed. Receipt docs/receipts/hostile-validator-20260822T001811Z.md OverallVerdict AGREE FailCount 0.' }
)

Invoke-Plugin -Method 'client.SessionLog.AppendDialogAsync' -Params @{
    agent = $agent
    sessionId = $sessionId
    requestId = $requestId
    items = $dialogItems
} -Name 'sl-dialog'

$actions = @(
    @{ order = 1; description = 'Executed add-profile; read 18 profile markdown files'; type = 'read'; status = 'completed'; filePath = 'C:\Users\kingd\.claude\profile\PROFILE.md' }
    @{ order = 2; description = 'Health nonce f803eb09c53a496fafada1be18070c48 echoed; plugin Test-MarkerSignature true; BeginTurnAsync turnId 42814'; type = 'read'; status = 'completed'; filePath = 'F:\GitHub\McpServer\AGENTS-README-FIRST.yaml' }
    @{ order = 3; description = 'Re-ran PluginSessionLogCatalogTests Failed 6 Passed 0 Skipped 0; LoadAndValidate not implemented'; type = 'test'; status = 'completed'; filePath = 'tests/McpServer.PluginIntegration.Tests/PluginSessionLogCatalogTests.cs' }
    @{ order = 4; description = 'Live todo_get PLAN-PLUGINHANDOFF-001 done false; MCP-PLUGININT-001 done false P4 task done false'; type = 'read'; status = 'completed'; filePath = 'docs/receipts/_hv-c-red-p4-r2/todo-plan-pluginhandoff-001.txt' }
    @{ order = 5; description = 'Wrote hostile receipt pair 20260822T001811Z OverallVerdict AGREE FailCount 0'; type = 'create'; status = 'completed'; filePath = 'docs/receipts/hostile-validator-20260822T001811Z.md' }
    @{ order = 6; description = 'AGREE because prior C3/D5 holes are closed; tests remain red; PLAN stays not done'; type = 'design_decision'; status = 'completed'; filePath = '' }
)

Invoke-Plugin -Method 'client.SessionLog.PatchTurnAsync' -Params @{
    agent = $agent
    sessionId = $sessionId
    requestId = $requestId
    turn = @{
        requestId = $requestId
        queryTitle = 'Hostile re-review C-red-P4 catalog AC1 pinning'
        actions = $actions
        designDecisions = @(
            'Score C-red-P4 rereview from independently re-run CatalogTests, live MCP store, prior DISAGREE timestamps, and catalog-path sibling probe, not implementer narrative.'
            'AGREE: remediations pin TR-MCP-PLUGININT-001 AC1 catalog fields. Named tests still fail via not implemented. Do not start claiming P4 green. Do not mark PLAN/PLUGININT done.'
        )
        tags = @('hostile-validator','PLAN-PLUGINHANDOFF-001','MCP-PLUGININT-001','C-red-P4','FR-MCP-PLUGININT-001','TR-MCP-PLUGININT-001','TEST-MCP-PLUGININT-001')
        contextList = @('docs/plans/PLAN-PLUGINHANDOFF-001.md','tests/McpServer.PluginIntegration.Tests/PluginSessionLogCatalogTests.cs','tests/McpServer.PluginIntegration.Tests/PluginSessionLogCatalog.cs','tests/McpServer.PluginIntegration.Tests/PluginSessionLogScenario.cs','tests/McpServer.PluginIntegration.Tests/scenarios/plugin-sessionlog-scenarios.json','docs/receipts/hostile-validator-20260821T235922Z.md','docs/receipts/hostile-validator-20260822T001811Z.md')
        interpretation = 'Re-review Phase C-red-P4 after DISAGREE. Review only. Do not implement. Do not mark TODOs done.'
    }
} -Name 'sl-patch'

Invoke-Plugin -Method 'client.SessionLog.CompleteTurnAsync' -Params @{
    agent = $agent
    sessionId = $sessionId
    requestId = $requestId
    payload = @{
        requestId = $requestId
        status = 'completed'
        response = 'Hostile C-red-P4 re-review AGREE. Receipt docs/receipts/hostile-validator-20260822T001811Z.md. PluginSessionLogCatalogTests Failed 6 Passed 0 Skipped 0. LoadAndValidate not implemented. Prior C3/D5 remediations hold. PLAN-PLUGINHANDOFF-001 and MCP-PLUGININT-001 remain done false. Do not claim P4 green.'
        queryTitle = 'Hostile re-review C-red-P4 catalog AC1 pinning'
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
