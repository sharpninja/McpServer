#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$out = 'F:\GitHub\McpServer\docs\receipts\_hv-c-red-p15'
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
$sessionId = 'GrokSubagentHostile-20260822T052803Z-c-red-p15'
$requestId = 'req-20260822T052803Z-001-hostile-c-red-p15'
$now = [DateTime]::UtcNow.ToString('o')
$receiptMdPath = 'F:\GitHub\McpServer\docs\receipts\hostile-validator-20260822T053539Z.md'
$receiptJsonPath = 'F:\GitHub\McpServer\docs\receipts\hostile-validator-20260822T053539Z.json'

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

$receiptMd = Get-Item -LiteralPath $receiptMdPath
$receiptJson = Get-Item -LiteralPath $receiptJsonPath
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
    FilterFailed = $jsonObj.TestResults.FilterConsoleFailed
    FilterPassed = $jsonObj.TestResults.FilterConsolePassed
    FilterSkipped = $jsonObj.TestResults.FilterConsoleSkipped
}
$proof | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $out 'receipt-on-disk.json') -Encoding utf8

$todoYamlStatus = git -C 'F:\GitHub\McpServer' status --porcelain -- docs/Project/TODO.yaml docs/todo.yaml
[ordered]@{ TodoYamlPorcelain = [string]$todoYamlStatus } | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $out 'todo-yaml-git-status-after.json') -Encoding utf8

$dialogItems = @(
    @{ timestamp = $now; role = 'model'; category = 'observation'; content = 'Independent P15 filter ExitCode 1. Console Failed! Failed 24 Passed 0 Skipped 0 Total 24 Duration 4 m 20 s. TRX executed 24 passed 0 failed 24 notExecuted 0. Success 8 FailsafePathVerified false; FailedSubmit 8 ExecuteFailedSubmitAsync not implemented; Retry 8 RetryFailedSubmitAsync not implemented. list-tests P15 8+8+8 P16 0 Skip 0. Live todo_get PLAN-PLUGINHANDOFF-001 done false; MCP-PLUGININT-001 done false P15 done false. V4 path ResolveFailsafePendingDirectory matches Get-McpFailsafeDir. Prior C-green-P14 AGREE docs/receipts/hostile-validator-20260822T050748Z.md Passed 62 P15 absent.' }
    @{ timestamp = $now; role = 'model'; category = 'decision'; content = 'Decision: AGREE C-red-P15. Three named theories exist with eight InlineData rows each, no Skip, map remaining TEST-MCP-PLUGININT-001 AC2 isolation plus failsafe, independently Failed 24 Passed 0 Skipped 0. P16 AiTheory absent. PLAN/PLUGININT remain done false. Consequence: parent may start P15 green; do not mark PLAN or PLUGININT done; do not write P16. Alternatives rejected: FAIL because Retry does not seed a sibling pending file (residual, tests currently fail closed); DISAGREE because homemade HMAC mismatched (plugin Test-MarkerSignature true is the contract); requiring full PluginIntegration suite mixed green plus red (C-red named filter is the gate).' }
)

Invoke-Plugin -Method 'client.SessionLog.AppendDialogAsync' -Params @{
    agent = $agent
    sessionId = $sessionId
    requestId = $requestId
    items = $dialogItems
} -Name 'sl-dialog'

$actions = @(
    @{ order = 1; description = 'Executed add-profile; read 18 profile markdown files'; type = 'read'; status = 'completed'; filePath = 'C:\Users\kingd\.claude\profile\PROFILE.md' }
    @{ order = 2; description = 'Health nonce nonce-hv-20260822052804-85089 echoed; plugin Test-MarkerSignature true; BeginTurnAsync turnId 42894'; type = 'read'; status = 'completed'; filePath = 'F:\GitHub\McpServer\AGENTS-README-FIRST.yaml' }
    @{ order = 3; description = 'Independent P15 filter Failed 24 Passed 0 Skipped 0 Total 24 Duration 4 m 20 s; success FailsafePathVerified false; failed submit and retry not implemented'; type = 'test'; status = 'completed'; filePath = 'tests/McpServer.PluginIntegration.Tests/PluginSessionLogWorkflowAdapterTests.cs' }
    @{ order = 4; description = 'Listed PluginIntegration P15 Success 8 FailedSubmit 8 Retry 8; P16 0; adapter FailsafePathVerified not assigned; ExecuteFailedSubmitAsync and RetryFailedSubmitAsync throw'; type = 'test'; status = 'completed'; filePath = 'tests/McpServer.PluginIntegration.Tests/PluginSessionLogWorkflowAdapter.cs' }
    @{ order = 5; description = 'Live todo_get PLAN-PLUGINHANDOFF-001 done false; MCP-PLUGININT-001 done false P15 done false; getFr/getTr/getTest/listMappings for PLUGININT-001'; type = 'read'; status = 'completed'; filePath = 'docs/receipts/_hv-c-red-p15/todo-plan.txt' }
    @{ order = 6; description = 'Wrote hostile receipt pair 20260822T053539Z OverallVerdict AGREE FailCount 0 PassCount 17'; type = 'create'; status = 'completed'; filePath = 'docs/receipts/hostile-validator-20260822T053539Z.md' }
    @{ order = 7; description = 'AGREE C-red-P15: three named theories eight rows each currently fail, no Skip, P16 absent, V4 failsafe path encoded, PLAN/PLUGININT remain done false. Do not implement P15 green. This AGREE is not plan closeout.'; type = 'design_decision'; status = 'completed'; filePath = '' }
)

Invoke-Plugin -Method 'client.SessionLog.PatchTurnAsync' -Params @{
    agent = $agent
    sessionId = $sessionId
    requestId = $requestId
    turn = @{
        requestId = $requestId
        queryTitle = 'Hostile C-red-P15 failsafe pending isolation'
        actions = $actions
        designDecisions = @(
            'Score C-red-P15 from independently re-run P15 filter, --list-tests, live MCP store, C-green-P14 AGREE, adapter stubs, and V4 path resolver, not implementer narrative.'
            'AGREE: three named Theories exist with eight InlineData PluginHostKind rows, no Skip, map remaining TEST-MCP-PLUGININT-001 AC2 isolation plus failsafe, Failed 24 Passed 0 Skipped 0. P16 names absent. PLAN and PLUGININT remain done false. Do not mark PLAN/PLUGININT done. Do not implement P15 green. Do not write P16.'
        )
        tags = @('hostile-validator','PLAN-PLUGINHANDOFF-001','MCP-PLUGININT-001','C-red-P15','FR-MCP-PLUGININT-001','TR-MCP-PLUGININT-001','TEST-MCP-PLUGININT-001')
        contextList = @('docs/plans/PLAN-PLUGINHANDOFF-001.md','tests/McpServer.PluginIntegration.Tests/PluginSessionLogWorkflowAdapterTests.cs','tests/McpServer.PluginIntegration.Tests/PluginSessionLogWorkflowAdapter.cs','docs/receipts/hostile-validator-20260822T050748Z.md','docs/receipts/hostile-validator-20260822T053539Z.md')
        interpretation = 'Hostile review Phase C-red-P15 ONLY after cited C-green-P14 AGREE. Review only. Do not implement P15 green. Do not write P16. Do not mark TODOs done.'
        filesModified = @('docs/receipts/hostile-validator-20260822T053539Z.md','docs/receipts/hostile-validator-20260822T053539Z.json')
    }
} -Name 'sl-patch'

Invoke-Plugin -Method 'client.SessionLog.CompleteTurnAsync' -Params @{
    agent = $agent
    sessionId = $sessionId
    requestId = $requestId
    payload = @{
        requestId = $requestId
        status = 'completed'
        response = 'Hostile C-red-P15 review AGREE. Receipt docs/receipts/hostile-validator-20260822T053539Z.md. P15 filter Failed 24 Passed 0 Skipped 0. Success rows FailsafePathVerified false. FailedSubmit and Retry throw not implemented. P16 names absent. PLAN-PLUGINHANDOFF-001 and MCP-PLUGININT-001 remain done false. Do not implement P15 green. Do not write P16. This AGREE is not plan closeout.'
        queryTitle = 'Hostile C-red-P15 failsafe pending isolation'
    }
} -Name 'sl-complete'

Invoke-Plugin -Method 'client.SessionLog.QueryAsync' -Params @{
    agent = $agent
    sessionId = $sessionId
    limit = 20
} -Name 'sl-query-sid-after'

Invoke-Plugin -Method 'workflow.sessionlog.queryHistory' -Params @{
    agent = $agent
    limit = 10
} -Name 'sl-query-history-after'

Write-Output 'FINISH_SESSION_DONE'
