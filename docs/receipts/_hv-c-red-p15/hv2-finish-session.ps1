#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$out = 'F:\GitHub\McpServer\docs\receipts\_hv-c-red-p15\hv2'
$plugin = 'F:\GitHub\mcpserver-grok-plugin\lib\Invoke-McpPlugin.ps1'
$cacheRoot = Join-Path $out 'plugin-cache'
Set-Location -LiteralPath 'F:\GitHub\McpServer'
$env:MCP_WORKSPACE_PATH = 'F:\GitHub\McpServer'
$env:MCPSERVER_WORKSPACE_PATH = 'F:\GitHub\McpServer'
$env:PLUGIN_AGENT_NAME = 'GrokSubagentHostile'
$env:MCP_AGENT_NAME = 'GrokSubagentHostile'
$env:GROK_PLUGIN_ROOT = 'F:\GitHub\mcpserver-grok-plugin'
$env:MCP_PLUGIN_ROOT = 'F:\GitHub\mcpserver-grok-plugin'

$agent = 'GrokSubagentHostile'
$sessionId = 'GrokSubagentHostile-20260822T052958Z-pluginhandoff-p15red'
$requestId = 'req-20260822T052958Z-001-hostile-c-red-p15-failsafe'
$now = [DateTime]::UtcNow.ToString('o')
$receiptMdPath = 'F:\GitHub\McpServer\docs\receipts\hostile-validator-20260822T060633Z.md'
$receiptJsonPath = 'F:\GitHub\McpServer\docs\receipts\hostile-validator-20260822T060633Z.json'

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
}
$proof | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $out 'receipt-on-disk.json') -Encoding utf8

$todoYamlStatus = git -C 'F:\GitHub\McpServer' status --porcelain -- docs/Project/TODO.yaml docs/todo.yaml
[ordered]@{ TodoYamlPorcelain = [string]$todoYamlStatus } | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $out 'todo-yaml-git-status-after.json') -Encoding utf8

$dialogItems = @(
    @{ timestamp = $now; role = 'model'; category = 'observation'; content = 'Independent P15 filter run3 Passed 24 Failed 0 Skipped 0 Total 24 ExitCode 0. First isolated run against 05:15:12 throw-not-implemented tree Failed 22 Passed 0 then testhost crash. Adapter/tests rewritten 2026-08-22T05:40:18Z during this C-red review and now set FailsafePathVerified true. Live todo_get PLAN-PLUGINHANDOFF-001 done false; MCP-PLUGININT-001 done false; P15 done false; combined C P14/P15 task done false. TEST AC isSatisfied false.' }
    @{ timestamp = $now; role = 'model'; category = 'decision'; content = 'Decision: DISAGREE C-red-P15. Consequence: do not start or continue P15 green from this gate; green already mixed in before AGREE. Alternatives rejected: AGREE because first run was 22-fail red (incomplete crash and current tree is green); treat run3 Passed 24 as C-green (wrong phase). Affected: PLAN-PLUGINHANDOFF-001 combined C P14/P15 task stays open; MCP-PLUGININT-001 P15 stays done false.' }
)

Invoke-Plugin -Method 'client.SessionLog.AppendDialogAsync' -Params @{
    agent = $agent
    sessionId = $sessionId
    requestId = $requestId
    items = $dialogItems
} -Name 'sl-dialog'

$actions = @(
    @{ order = 1; description = 'Executed add-profile; read 18 profile markdown files'; type = 'read'; status = 'completed'; filePath = 'C:\Users\kingd\.claude\profile\PROFILE.md' }
    @{ order = 2; description = 'Test-MarkerSignature true via plugins/core/lib-ps/marker-resolver.ps1; health nonce nonce-hv2-20260822053004-22568 echoed'; type = 'read'; status = 'completed'; filePath = 'F:\GitHub\McpServer\AGENTS-README-FIRST.yaml' }
    @{ order = 3; description = 'First isolated P15 filter Failed 22 Passed 0 then testhost crash against throw-not-implemented adapter 05:15:12Z'; type = 'test'; status = 'completed'; filePath = 'docs/receipts/_hv-c-red-p15/hv2/trx-p15/kingd_PAYTON-LEGION2_2026-08-22_00_36_58_net10.0.trx' }
    @{ order = 4; description = 'Run3 isolated P15 filter ExitCode 0 Passed 24 Failed 0 Skipped 0 after adapter/tests rewrite 05:40:18Z'; type = 'test'; status = 'completed'; filePath = 'docs/receipts/_hv-c-red-p15/hv2/trx-p15-run3/kingd_PAYTON-LEGION2_2026-08-22_00_57_54_net10.0.trx' }
    @{ order = 5; description = 'Live todo_get PLAN-PLUGINHANDOFF-001 done false; MCP-PLUGININT-001 done false P15 done false; getFr/getTr/getTest/listMappings'; type = 'read'; status = 'completed'; filePath = 'docs/receipts/_hv-c-red-p15/hv2/todo-plan-final.txt' }
    @{ order = 6; description = 'Wrote hostile receipt pair 20260822T060633Z OverallVerdict DISAGREE FailCount 8'; type = 'create'; status = 'completed'; filePath = 'docs/receipts/hostile-validator-20260822T060633Z.md' }
    @{ order = 7; description = 'DISAGREE C-red-P15: green-before-red. Independent filter Passed 24. Adapter rewritten during red gate. PLAN/PLUGININT remain done false. Do not authorize P16.'; type = 'design_decision'; status = 'completed'; filePath = '' }
)

Invoke-Plugin -Method 'client.SessionLog.PatchTurnAsync' -Params @{
    agent = $agent
    sessionId = $sessionId
    requestId = $requestId
    turn = @{
        requestId = $requestId
        queryTitle = 'Hostile C-red-P15 failsafe pending isolation red gate'
        actions = $actions
        designDecisions = @(
            'Score C-red-P15 from independently re-run P15 filter plus live MCP store and adapter timestamps, not implementer narrative.'
            'DISAGREE: current independent filter Passed 24 Failed 0. Adapter/tests rewritten 05:40:18 during this red review. First isolated run was 22 fail then crash against throw-not-implemented. Green-before-red. PLAN and PLUGININT remain done false. Do not mark PLAN/PLUGININT done. This DISAGREE does not authorize P15 green or P16.'
        )
        tags = @('hostile-validator','PLAN-PLUGINHANDOFF-001','MCP-PLUGININT-001','C-red-P15','FR-MCP-PLUGININT-001','TR-MCP-PLUGININT-001','TEST-MCP-PLUGININT-001')
        contextList = @('docs/plans/PLAN-PLUGINHANDOFF-001.md','tests/McpServer.PluginIntegration.Tests/PluginSessionLogWorkflowAdapterTests.cs','tests/McpServer.PluginIntegration.Tests/PluginSessionLogWorkflowAdapter.cs','docs/receipts/hostile-validator-20260822T044818Z.md','docs/receipts/hostile-validator-20260822T050748Z.md','docs/receipts/hostile-validator-20260822T060633Z.md')
        interpretation = 'Hostile review Phase C-red-P15 ONLY after cited C-green-P14 AGREE. Review only. Do not implement P15 green. Do not mark TODOs done.'
        filesModified = @('docs/receipts/hostile-validator-20260822T060633Z.md','docs/receipts/hostile-validator-20260822T060633Z.json')
        status = 'in_progress'
        response = 'Hostile C-red-P15 review DISAGREE. Receipt docs/receipts/hostile-validator-20260822T060633Z.md. Independent P15 filter Passed 24 Failed 0 Skipped 0 after adapter rewrite 05:40:18Z. First isolated run Failed 22 then crash. Green-before-red. PLAN-PLUGINHANDOFF-001 and MCP-PLUGININT-001 remain done false.'
    }
} -Name 'sl-patch'

Invoke-Plugin -Method 'client.SessionLog.CompleteTurnAsync' -Params @{
    agent = $agent
    sessionId = $sessionId
    requestId = $requestId
    payload = @{
        requestId = $requestId
        status = 'completed'
        response = 'Hostile C-red-P15 review DISAGREE. Receipt docs/receipts/hostile-validator-20260822T060633Z.md. Independent P15 filter Passed 24 Failed 0 Skipped 0. Adapter/tests rewritten 05:40:18Z during this red gate. First isolated run Failed 22 then crash against throw-not-implemented. PLAN-PLUGINHANDOFF-001 and MCP-PLUGININT-001 remain done false. This DISAGREE does not authorize P15 green or P16 and is not plan closeout.'
        queryTitle = 'Hostile C-red-P15 failsafe pending isolation red gate'
    }
} -Name 'sl-complete'

Invoke-Plugin -Method 'client.SessionLog.QueryAsync' -Params @{
    agent = $agent
    sessionId = $sessionId
    limit = 5
} -Name 'sl-query-after'

Invoke-Plugin -Method 'workflow.sessionlog.queryHistory' -Params @{
    agent = $agent
    sessionId = $sessionId
    limit = 5
    offset = 0
} -Name 'sl-query-history-after'

Write-Output 'FINISH_SESSION_DONE'
