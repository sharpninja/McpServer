#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$out = 'F:\GitHub\McpServer\docs\receipts\_hv-c-red-p11'
$plugin = 'F:\GitHub\mcpserver-grok-plugin\lib\Invoke-McpPlugin.ps1'
$cacheRoot = Join-Path $out 'plugin-cache'
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
$sessionId = 'GrokSubagentHostile-20260822T023011Z-c-red-p11'
$requestId = 'req-20260822T023011Z-001-hostile-c-red-p11'
$now = [DateTime]::UtcNow.ToString('o')

function Invoke-Plugin {
    param([string]$Method, [hashtable]$Params, [string]$Name)
    $outFile = Join-Path $out ($Name + '.txt')
    $errFile = Join-Path $out ($Name + '.err.txt')
    try {
        $result = & $plugin -Command Invoke -Method $Method -ParamsObject $Params -WorkspacePath 'F:\GitHub\McpServer' -CacheRoot $cacheRoot -TimeoutSeconds 120 2> $errFile
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

$receiptMd = Get-Item -LiteralPath 'F:\GitHub\McpServer\docs\receipts\hostile-validator-20260822T023808Z.md'
$receiptJson = Get-Item -LiteralPath 'F:\GitHub\McpServer\docs\receipts\hostile-validator-20260822T023808Z.json'
$mdText = Get-Content -LiteralPath $receiptMd.FullName -Raw
$jsonObj = Get-Content -LiteralPath $receiptJson.FullName -Raw | ConvertFrom-Json
$proof = [ordered]@{
    MdExists = $true
    MdLength = $receiptMd.Length
    MdLastWriteTimeUtc = $receiptMd.LastWriteTimeUtc.ToString('o')
    MdHasAgree = [bool]($mdText -match '(?m)^OverallVerdict:\s*AGREE\s*$')
    MdHasDisagree = [bool]($mdText -match '(?m)^OverallVerdict:\s*DISAGREE\s*$')
    MdEmDash = $mdText.Contains([char]0x2014)
    MdEnDash = $mdText.Contains([char]0x2013)
    JsonExists = $true
    JsonLength = $receiptJson.Length
    JsonLastWriteTimeUtc = $receiptJson.LastWriteTimeUtc.ToString('o')
    JsonOverallVerdict = [string]$jsonObj.OverallVerdict
    JsonFailCount = $jsonObj.FailCount
    JsonPassCount = $jsonObj.PassCount
    JsonClaimCount = @($jsonObj.Claims).Count
}
$proof | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $out 'receipt-on-disk.json') -Encoding utf8

$todoYamlStatus = git -C 'F:\GitHub\McpServer' status --porcelain -- docs/Project/TODO.yaml
[ordered]@{ TodoYamlPorcelain = [string]$todoYamlStatus } | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $out 'todo-yaml-git-status.json') -Encoding utf8

$dialogItems = @(
    @{ timestamp = $now; role = 'model'; category = 'observation'; content = 'Independent re-run Theory_EachScenario_FailsUntilAdapterOperational Failed 8 Passed 0 Skipped 0. Full PluginIntegration.Tests Failed 8 Passed 30 Skipped 0 Total 38. ExecuteCanonicalTurnAsync throws PluginSessionLogWorkflowAdapter.ExecuteCanonicalTurnAsync is not implemented. P12/P13 named theories MatchCount 0 in tests/src. Live todo_get PLAN-PLUGINHANDOFF-001 done false; MCP-PLUGININT-001 done false P11/P12/P13 done false.' }
    @{ timestamp = $now; role = 'model'; category = 'decision'; content = 'Decision: AGREE C-red-P11. Attacks not sustained: missing eight-row theory; Skip on P11; ExecuteCanonicalTurnAsync implemented and rows pass; independent filter not Failed 8 Passed 0 Skipped 0; full project not Failed 8 Passed 30 Skipped 0; P12/P13 greens mixed in; PLAN or PLUGININT marked done. Consequence: C-red-P11 gate may proceed to P12-P13 green only after this AGREE; do not mark PLAN or PLUGININT done; do not require P12-P13 green. Alternatives rejected: DISAGREE because homemade HMAC is false (plugin Test-MarkerSignature true); DISAGREE because D2 HandoffIngestionService.cs is dirty (not P12/P13 mix).' }
    @{ timestamp = $now; role = 'model'; category = 'observation'; content = 'C-green-P7-P10 AGREE on disk. Adapter files LastWriteTime after 2026-08-22T02:07:56Z. Plugin signature true. Health nonce a7c9c9a58c2a42a9b7fdc5e7c2ad1aa9 echoed. Receipt docs/receipts/hostile-validator-20260822T023808Z.md OverallVerdict AGREE FailCount 0 PassCount 21.' }
)

Invoke-Plugin -Method 'client.SessionLog.AppendDialogAsync' -Params @{
    agent = $agent
    sessionId = $sessionId
    requestId = $requestId
    items = $dialogItems
} -Name 'sl-dialog'

$actions = @(
    @{ order = 1; description = 'Executed add-profile; read 18 profile markdown files'; type = 'read'; status = 'completed'; filePath = 'C:\Users\kingd\.claude\profile\PROFILE.md' }
    @{ order = 2; description = 'Health nonce a7c9c9a58c2a42a9b7fdc5e7c2ad1aa9 echoed; plugin Test-MarkerSignature true; BeginTurnAsync turnId 42861'; type = 'read'; status = 'completed'; filePath = 'F:\GitHub\McpServer\AGENTS-README-FIRST.yaml' }
    @{ order = 3; description = 'Re-ran Theory_EachScenario_FailsUntilAdapterOperational Failed 8 Passed 0 Skipped 0; ExecuteCanonicalTurnAsync throws not implemented'; type = 'test'; status = 'completed'; filePath = 'tests/McpServer.PluginIntegration.Tests/PluginSessionLogWorkflowAdapter.cs' }
    @{ order = 4; description = 'Full PluginIntegration.Tests Failed 8 Passed 30 Skipped 0 Total 38; P12/P13 named tests absent from tests/src'; type = 'test'; status = 'completed'; filePath = 'tests/McpServer.PluginIntegration.Tests/PluginSessionLogWorkflowAdapterTests.cs' }
    @{ order = 5; description = 'Live todo_get PLAN-PLUGINHANDOFF-001 done false; MCP-PLUGININT-001 done false P11 P12 P13 done false'; type = 'read'; status = 'completed'; filePath = 'docs/receipts/_hv-c-red-p11/todo-plan.txt' }
    @{ order = 6; description = 'Wrote hostile receipt pair 20260822T023808Z OverallVerdict AGREE FailCount 0 PassCount 21'; type = 'create'; status = 'completed'; filePath = 'docs/receipts/hostile-validator-20260822T023808Z.md' }
    @{ order = 7; description = 'AGREE because eight-row P11 theory exists without Skip, adapter throws not implemented, independent filter Failed 8 Passed 0 Skipped 0, full project Failed 8 Passed 30 Skipped 0, P12/P13 absent, PLAN stays not done'; type = 'design_decision'; status = 'completed'; filePath = '' }
)

Invoke-Plugin -Method 'client.SessionLog.PatchTurnAsync' -Params @{
    agent = $agent
    sessionId = $sessionId
    requestId = $requestId
    turn = @{
        requestId = $requestId
        queryTitle = 'Hostile C-red-P11 plugin Session Log workflow adapter reds'
        actions = $actions
        designDecisions = @(
            'Score C-red-P11 from independently re-run Theory_EachScenario_FailsUntilAdapterOperational, live MCP store, C-green-P7-P10 AGREE timestamps, and adapter source, not implementer narrative.'
            'AGREE: ExecuteCanonicalTurnAsync throws not implemented; named P11 theory exists with eight InlineData rows and no Skip; filter Failed 8 Passed 0 Skipped 0; full project Failed 8 Passed 30 Skipped 0 Total 38; P12/P13 theories absent from tests/src; PLAN and PLUGININT remain done false. Do not mark PLAN/PLUGININT done. Do not require P12-P13 green.'
        )
        tags = @('hostile-validator','PLAN-PLUGINHANDOFF-001','MCP-PLUGININT-001','C-red-P11','FR-MCP-PLUGININT-001','TR-MCP-PLUGININT-001','TEST-MCP-PLUGININT-001')
        contextList = @('docs/plans/PLAN-PLUGINHANDOFF-001.md','tests/McpServer.PluginIntegration.Tests/PluginSessionLogWorkflowAdapterTests.cs','tests/McpServer.PluginIntegration.Tests/PluginSessionLogWorkflowAdapter.cs','docs/receipts/hostile-validator-20260822T020756Z.md','docs/receipts/hostile-validator-20260822T023808Z.md')
        interpretation = 'Hostile review Phase C-red-P11 after C-green-P7-P10 AGREE. Review only. Do not implement. Do not mark TODOs done. Do not require P12-P13 green.'
        filesModified = @('docs/receipts/hostile-validator-20260822T023808Z.md','docs/receipts/hostile-validator-20260822T023808Z.json')
    }
} -Name 'sl-patch'

Invoke-Plugin -Method 'client.SessionLog.CompleteTurnAsync' -Params @{
    agent = $agent
    sessionId = $sessionId
    requestId = $requestId
    payload = @{
        requestId = $requestId
        status = 'completed'
        response = 'Hostile C-red-P11 review AGREE. Receipt docs/receipts/hostile-validator-20260822T023808Z.md. Theory_EachScenario_FailsUntilAdapterOperational Failed 8 Passed 0 Skipped 0. Full PluginIntegration.Tests Failed 8 Passed 30 Skipped 0 Total 38. ExecuteCanonicalTurnAsync throws not implemented. P12/P13 absent. Attacks not sustained. PLAN-PLUGINHANDOFF-001 and MCP-PLUGININT-001 remain done false. Do not require P12-P13 green. This AGREE is not plan closeout.'
        queryTitle = 'Hostile C-red-P11 plugin Session Log workflow adapter reds'
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
    AfterQueryJsonFailCount = ((Get-Content -LiteralPath $receiptJson2.FullName -Raw | ConvertFrom-Json).FailCount)
    TodoYamlPorcelainAfter = [string]$todoYamlStatus
} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $out 'receipt-on-disk-after-query.json') -Encoding utf8

Write-Output 'FINISH_SESSION_DONE'
