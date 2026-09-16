#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'

$out = 'F:\GitHub\McpServer\docs\receipts\_hv-c-red-p16'
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
$sessionId = 'GrokSubagentHostile-20260822T072515Z-pluginhandoff-p16red'
$requestId = 'req-20260822T072515Z-001-hostile-c-red-p16-aitheory'
$now = [DateTime]::UtcNow.ToString('o')
$receiptMdPath = 'F:\GitHub\McpServer\docs\receipts\hostile-validator-20260822T073115Z.md'
$receiptJsonPath = 'F:\GitHub\McpServer\docs\receipts\hostile-validator-20260822T073115Z.json'

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
    SessionId = [string]$jsonObj.SessionId
    RequestId = [string]$jsonObj.RequestId
}
$proof | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $out 'receipt-on-disk-finish.json') -Encoding utf8

$dialogItems = @(
    @{ timestamp = $now; role = 'model'; category = 'observation'; content = 'Independent P16 filter ExitCode 1. Console Test Run Failed. Total tests 8 Failed 8 Total time 2.2379 Seconds. TRX executed 8 passed 0 failed 8 notExecuted 0 p16Failed 8 p16Passed 0 p16Skipped 0 unimplementedCount 8. --list-tests AiTheory 8 P17 0 P18 0 TotalAvailable 94. Live todo_get PLAN-PLUGINHANDOFF-001 done false; MCP-PLUGININT-001 done false P16 done false. Receipt docs/receipts/hostile-validator-20260822T073115Z.md OverallVerdict AGREE FailCount 0.' }
    @{ timestamp = $now; role = 'model'; category = 'decision'; content = 'Decision: AGREE C-red-P16. Named theory exists with eight InlineData rows, no Skip, maps TEST-MCP-PLUGININT-001 AC3, independently Failed 8 Passed 0 Skipped 0, EvaluateAsync throws not implemented. P17 names absent. PLAN/PLUGININT remain done false. Consequence: parent may start P17-P18 green; do not mark PLAN or PLUGININT done; do not start P19. Alternatives rejected: DISAGREE because sibling used the same collector folder (this review used unique ResultsDirectory 072653Z); FAIL synthetic redacted string versus persisted artifact (parent locked catalog plus redacted string for this red gate).' }
)

Invoke-Plugin -Method 'client.SessionLog.AppendDialogAsync' -Params @{
    agent = $agent
    sessionId = $sessionId
    requestId = $requestId
    items = $dialogItems
} -Name 'sl-dialog'

$actions = @(
    @{ order = 1; description = 'Executed add-profile; read 18 profile markdown files'; type = 'read'; status = 'completed'; filePath = 'C:\Users\kingd\.claude\profile\PROFILE.md' }
    @{ order = 2; description = 'Health nonce nonce-hv-20260822072516-9543 echoed; plugin Test-MarkerSignature true; BeginTurnAsync turnId 42918'; type = 'read'; status = 'completed'; filePath = 'F:\GitHub\McpServer\AGENTS-README-FIRST.yaml' }
    @{ order = 3; description = 'Independent --list-tests ExitCode 0; AiTheory_Agent_RequiresValidJsonFields 8 rows; P17 0; P18 0; TotalAvailable 94'; type = 'test'; status = 'completed'; filePath = 'tests/McpServer.PluginIntegration.Tests/PluginSessionLogAiTheoryTests.cs' }
    @{ order = 4; description = 'Independent P16 filter Failed 8 Passed 0 Skipped 0 Total 8 Duration 2.2379 Seconds; EvaluateAsync not implemented; unique ResultsDirectory results-20260822T072653Z'; type = 'test'; status = 'completed'; filePath = 'tests/McpServer.PluginIntegration.Tests/AiStrategyFixture.cs' }
    @{ order = 5; description = 'Live todo_get PLAN-PLUGINHANDOFF-001 done false; MCP-PLUGININT-001 done false P16 done false; getFr/getTr/getTest/listMappings for PLUGININT-001'; type = 'read'; status = 'completed'; filePath = 'docs/receipts/_hv-c-red-p16/todo-plan-final.txt' }
    @{ order = 6; description = 'Wrote hostile receipt pair 20260822T073115Z OverallVerdict AGREE FailCount 0 PassCount 20'; type = 'create'; status = 'completed'; filePath = 'docs/receipts/hostile-validator-20260822T073115Z.md' }
    @{ order = 7; description = 'AGREE C-red-P16: named theory eight rows fail, EvaluateAsync unimplemented, no Skip, no P17 mix-in, PLAN/PLUGININT remain done false. Do not implement P17-P18 in this review. This AGREE is not plan closeout.'; type = 'design_decision'; status = 'completed'; filePath = 'docs/plans/PLAN-PLUGINHANDOFF-001.md' }
)

Invoke-Plugin -Method 'client.SessionLog.PatchTurnAsync' -Params @{
    agent = $agent
    sessionId = $sessionId
    requestId = $requestId
    turn = @{
        requestId = $requestId
        queryTitle = 'Hostile C-red-P16 AiTheory companion red gate'
        actions = $actions
        designDecisions = @(
            'Score C-red-P16 from independently re-run P16 filter, --list-tests, live MCP store, C-green-P15 AGREE, and source files, not implementer narrative.'
            'AGREE: named theory exists with eight InlineData PluginHostKind rows, no Skip, maps TEST-MCP-PLUGININT-001 AC3, Failed 8 Passed 0 Skipped 0. EvaluateAsync throws not implemented. P17 names absent. PLAN and PLUGININT remain done false. Do not mark PLAN/PLUGININT done. Do not implement P17-P18 in this review.'
        )
        tags = @('hostile-validator','PLAN-PLUGINHANDOFF-001','MCP-PLUGININT-001','C-red-P16','FR-MCP-PLUGININT-001','TR-MCP-PLUGININT-001','TEST-MCP-PLUGININT-001')
        contextList = @('docs/plans/PLAN-PLUGINHANDOFF-001.md','tests/McpServer.PluginIntegration.Tests/PluginSessionLogAiTheoryTests.cs','tests/McpServer.PluginIntegration.Tests/AiStrategyFixture.cs','docs/receipts/hostile-validator-20260822T071202Z.md','docs/receipts/hostile-validator-20260822T073115Z.md')
        interpretation = 'Hostile review Phase C-red-P16 ONLY after cited C-green-P15 AGREE. Review only. Do not implement P17-P18. Do not mark TODOs done.'
        filesModified = @('docs/receipts/hostile-validator-20260822T073115Z.md','docs/receipts/hostile-validator-20260822T073115Z.json')
    }
} -Name 'sl-patch'

Invoke-Plugin -Method 'client.SessionLog.CompleteTurnAsync' -Params @{
    agent = $agent
    sessionId = $sessionId
    requestId = $requestId
    payload = @{
        requestId = $requestId
        status = 'completed'
        response = 'Hostile C-red-P16 review AGREE. Receipt docs/receipts/hostile-validator-20260822T073115Z.md. P16 filter Failed 8 Passed 0 Skipped 0. EvaluateAsync not implemented. P17 names absent. PLAN-PLUGINHANDOFF-001 and MCP-PLUGININT-001 remain done false. Do not implement P17-P18. This AGREE is not plan closeout.'
        queryTitle = 'Hostile C-red-P16 AiTheory companion red gate'
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
