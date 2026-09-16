#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'

$out = 'F:\GitHub\McpServer\docs\receipts\_hv-c-green-p15'
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
$sessionId = 'GrokSubagentHostile-20260822T062054Z-c-green-p15'
$requestId = 'req-20260822T062054Z-001-hostile-c-green-p15'
$now = [DateTime]::UtcNow.ToString('o')
$receiptMdPath = 'F:\GitHub\McpServer\docs\receipts\hostile-validator-20260822T071202Z.md'
$receiptJsonPath = 'F:\GitHub\McpServer\docs\receipts\hostile-validator-20260822T071202Z.json'

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
    FullFailed = $jsonObj.TestResults.FullConsoleFailed
    FullPassed = $jsonObj.TestResults.FullConsolePassed
    FullSkipped = $jsonObj.TestResults.FullConsoleSkipped
}
$proof | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $out 'receipt-on-disk-finish.json') -Encoding utf8

$todoYamlStatus = git -C 'F:\GitHub\McpServer' status --porcelain -- docs/Project/TODO.yaml docs/todo.yaml
[ordered]@{ TodoYamlPorcelain = [string]$todoYamlStatus } | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $out 'todo-yaml-git-status-final.json') -Encoding utf8

$dialogItems = @(
    @{ timestamp = $now; role = 'model'; category = 'observation'; content = 'Independent P15 filter ExitCode 0. Console Passed! Failed 0 Passed 24 Skipped 0 Total 24 Duration 5 m 43 s. Independent full PluginIntegration.Tests ExitCode 0. Console Passed! Failed 0 Passed 86 Skipped 0 Total 86 Duration 16 m 24 s. TRX executed 86 passed 86 failed 0 notExecuted 0 p15Passed 24 p16Count 0. Live todo_get PLAN-PLUGINHANDOFF-001 done false; MCP-PLUGININT-001 done false P15 done false. Receipt docs/receipts/hostile-validator-20260822T071202Z.md OverallVerdict AGREE FailCount 0.' }
    @{ timestamp = $now; role = 'model'; category = 'decision'; content = 'Decision: AGREE C-green-P15. Named theories exist with eight InlineData rows, no Skip, maps TEST-MCP-PLUGININT-001 AC2 failsafe, independently Failed 0 Passed 24 Skipped 0, full suite Failed 0 Passed 86 Skipped 0. P16 names absent. PLAN/PLUGININT remain done false. Consequence: parent may start P16 red; do not mark PLAN or PLUGININT done. Alternatives rejected: FAIL plant-file FailedSubmit (parent A1 described that write); DISAGREE because first full suite was killed (this review independently re-ran it).' }
)

Invoke-Plugin -Method 'client.SessionLog.AppendDialogAsync' -Params @{
    agent = $agent
    sessionId = $sessionId
    requestId = $requestId
    items = $dialogItems
} -Name 'sl-dialog'

$actions = @(
    @{ order = 1; description = 'Executed add-profile; read 18 profile markdown files'; type = 'read'; status = 'completed'; filePath = 'C:\Users\kingd\.claude\profile\PROFILE.md' }
    @{ order = 2; description = 'Health nonce nonce-hv-20260822062055-15239 echoed; plugin Test-MarkerSignature true; BeginTurnAsync retry turnId 42908'; type = 'read'; status = 'completed'; filePath = 'F:\GitHub\McpServer\AGENTS-README-FIRST.yaml' }
    @{ order = 3; description = 'Independent P15 filter Failed 0 Passed 24 Skipped 0 Total 24 Duration 5 m 43 s; all eight host kinds Passed'; type = 'test'; status = 'completed'; filePath = 'tests/McpServer.PluginIntegration.Tests/PluginSessionLogWorkflowAdapterTests.cs' }
    @{ order = 4; description = 'Independent full PluginIntegration.Tests Failed 0 Passed 86 Skipped 0 Total 86 Duration 16 m 24 s; P15 24 P16 0; first full tree killed by sibling run2 then rerun WMI PID 47568'; type = 'test'; status = 'completed'; filePath = 'tests/McpServer.PluginIntegration.Tests/PluginSessionLogWorkflowAdapter.cs' }
    @{ order = 5; description = 'Live todo_get PLAN-PLUGINHANDOFF-001 done false; MCP-PLUGININT-001 done false P15 done false; getFr/getTr/getTest/listMappings for PLUGININT-001'; type = 'read'; status = 'completed'; filePath = 'docs/receipts/_hv-c-green-p15/todo-plan-final.txt' }
    @{ order = 6; description = 'Wrote hostile receipt pair 20260822T071202Z OverallVerdict AGREE FailCount 0 PassCount 18'; type = 'create'; status = 'completed'; filePath = 'docs/receipts/hostile-validator-20260822T071202Z.md' }
    @{ order = 7; description = 'AGREE C-green-P15: named theories eight rows each pass, V4 failsafe write/verify/retry sibling retain, P16 absent, PLAN/PLUGININT remain done false. Do not implement P16. This AGREE is not plan closeout.'; type = 'design_decision'; status = 'completed'; filePath = '' }
)

Invoke-Plugin -Method 'client.SessionLog.PatchTurnAsync' -Params @{
    agent = $agent
    sessionId = $sessionId
    requestId = $requestId
    turn = @{
        requestId = $requestId
        queryTitle = 'Hostile C-green-P15 failsafe pending isolation'
        actions = $actions
        designDecisions = @(
            'Score C-green-P15 from independently re-run P15 filter, full PluginIntegration.Tests, --list-tests, live MCP store, C-red-P15 AGREE, and adapter failsafe methods, not implementer narrative.'
            'AGREE: named theories exist with eight InlineData PluginHostKind rows, no Skip, maps TEST-MCP-PLUGININT-001 AC2 failsafe, Failed 0 Passed 24 Skipped 0 and full suite Failed 0 Passed 86 Skipped 0. P16 names absent. PLAN and PLUGININT remain done false. Do not mark PLAN/PLUGININT done. Do not implement P16 in this review.'
        )
        tags = @('hostile-validator','PLAN-PLUGINHANDOFF-001','MCP-PLUGININT-001','C-green-P15','FR-MCP-PLUGININT-001','TR-MCP-PLUGININT-001','TEST-MCP-PLUGININT-001')
        contextList = @('docs/plans/PLAN-PLUGINHANDOFF-001.md','tests/McpServer.PluginIntegration.Tests/PluginSessionLogWorkflowAdapterTests.cs','tests/McpServer.PluginIntegration.Tests/PluginSessionLogWorkflowAdapter.cs','docs/receipts/hostile-validator-20260822T053539Z.md','docs/receipts/hostile-validator-20260822T071202Z.md')
        interpretation = 'Hostile review Phase C-green-P15 ONLY after cited C-red-P15 AGREE. Review only. Do not implement P16. Do not mark TODOs done.'
        filesModified = @('docs/receipts/hostile-validator-20260822T071202Z.md','docs/receipts/hostile-validator-20260822T071202Z.json')
    }
} -Name 'sl-patch'

Invoke-Plugin -Method 'client.SessionLog.CompleteTurnAsync' -Params @{
    agent = $agent
    sessionId = $sessionId
    requestId = $requestId
    payload = @{
        requestId = $requestId
        status = 'completed'
        response = 'Hostile C-green-P15 review AGREE. Receipt docs/receipts/hostile-validator-20260822T071202Z.md. P15 filter Passed 24 Failed 0 Skipped 0. Full PluginIntegration.Tests Passed 86 Failed 0 Skipped 0. P16 names absent. PLAN-PLUGINHANDOFF-001 and MCP-PLUGININT-001 remain done false. Do not implement P16. This AGREE is not plan closeout.'
        queryTitle = 'Hostile C-green-P15 failsafe pending isolation'
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
