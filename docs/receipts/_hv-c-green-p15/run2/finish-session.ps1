#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$out = 'F:\GitHub\McpServer\docs\receipts\_hv-c-green-p15\run2'
$plugin = 'F:\GitHub\mcpserver-grok-plugin\lib\Invoke-McpPlugin.ps1'
$cacheRoot = Join-Path $out 'plugin-cache'
$agent = 'GrokSubagentHostile'
$sessionId = (Get-Content -LiteralPath (Join-Path $out 'session-id.txt') -Raw).Trim()
$requestId = (Get-Content -LiteralPath (Join-Path $out 'request-id.txt') -Raw).Trim()

$env:MCP_WORKSPACE_PATH = 'F:\GitHub\McpServer'
$env:MCPSERVER_WORKSPACE_PATH = 'F:\GitHub\McpServer'
$env:PLUGIN_AGENT_NAME = 'GrokSubagentHostile'
$env:MCP_AGENT_NAME = 'GrokSubagentHostile'
$env:GROK_PLUGIN_ROOT = 'F:\GitHub\mcpserver-grok-plugin'
$env:MCP_PLUGIN_ROOT = 'F:\GitHub\mcpserver-grok-plugin'
$env:MCP_PLUGIN_HOST = 'grok'

function Invoke-Plugin {
    param([string]$Method, [hashtable]$Params, [string]$Name)
    $outFile = Join-Path $out ($Name + '.txt')
    $errFile = Join-Path $out ($Name + '.err.txt')
    try {
        $result = & $plugin -Command Invoke -Method $Method -ParamsObject $Params -WorkspacePath 'F:\GitHub\McpServer' -CacheRoot $cacheRoot -TimeoutSeconds 180 2> $errFile
        if ($null -eq $result) { $result = '' }
        if ($result -isnot [string]) { $result = ($result | Out-String) }
        Set-Content -LiteralPath $outFile -Value $result -Encoding utf8
        Write-Output ('OK ' + $Name)
    } catch {
        Set-Content -LiteralPath $outFile -Value ('ERROR ' + $_.Exception.ToString()) -Encoding utf8
        Write-Output ('FAIL ' + $Name + ' ' + $_.Exception.Message)
    }
}

$now = [DateTime]::UtcNow.ToString('o')
Invoke-Plugin -Method 'client.SessionLog.AppendDialogAsync' -Params @{
    agent = $agent
    sessionId = $sessionId
    requestId = $requestId
    items = @(
        @{ timestamp = $now; role = 'model'; category = 'observation'; content = 'Independent P15 filter ExitCode 0 Failed 0 Passed 24 Skipped 0 Total 24 Duration 4 m 45 s. TRX run2/results-20260822T063532Z. FailedSubmit does not assert root-id-named pending filename. Receipt docs/receipts/hostile-validator-20260822T064147Z.md OverallVerdict DISAGREE. PLAN and MCP-PLUGININT remain done false. This DISAGREE does not authorize C-red-P16.' }
        @{ timestamp = $now; role = 'model'; category = 'decision'; content = 'Decision: DISAGREE C-green-P15 because FailedSubmit lacks root-id filename assert required by MCP-PLUGININT-001 P15 task and theory RetainsRootIdPending, even though the 24-row filter is green. Consequence: do not mark PLAN or MCP-PLUGININT done; do not authorize C-red-P16. Alternatives: AGREE on filter-green only and record the assert as residual. Rejected: C-red 053539Z deferred that tightening to green; suite green is not AC coverage.' }
    )
} -Name 'sl-dialog-end'

$actions = @(
    @{ order = 1; description = 'add-profile executed; 18 non-skill profile markdown files read'; type = 'observation'; status = 'completed'; filePath = 'C:\Users\kingd\.claude\profile' }
    @{ order = 2; description = 'Test-MarkerSignature true; health nonce nonce-hv-cgreen-p15-20260822062813-61400 echoed'; type = 'observation'; status = 'completed'; filePath = 'F:\GitHub\McpServer\plugins\core\lib-ps\marker-resolver.ps1' }
    @{ order = 3; description = 'Independent P15 filter Failed 0 Passed 24 Skipped 0 Total 24 unique ResultsDirectory'; type = 'test'; status = 'completed'; filePath = 'docs/receipts/_hv-c-green-p15/run2/results-20260822T063532Z/kingd_PAYTON-LEGION2_2026-08-22_01_36_06_net10.0.trx' }
    @{ order = 4; description = 'Live todo_get PLAN-PLUGINHANDOFF-001 done false; MCP-PLUGININT-001 done false P15 done false; getFr/getTr/getTest/listMappings'; type = 'observation'; status = 'completed'; filePath = 'docs/receipts/_hv-c-green-p15/run2/todo-plan.txt' }
    @{ order = 5; description = 'Wrote hostile receipt pair 20260822T064147Z OverallVerdict DISAGREE FailCount 2'; type = 'create'; status = 'completed'; filePath = 'docs/receipts/hostile-validator-20260822T064147Z.md' }
    @{ order = 6; description = 'DISAGREE C-green-P15: FailedSubmit missing root-id filename assert. Filter green 24/24. PLAN/PLUGININT remain done false. Do not authorize C-red-P16.'; type = 'design_decision'; status = 'completed'; filePath = 'docs/plans/PLAN-PLUGINHANDOFF-001.md' }
)

Invoke-Plugin -Method 'client.SessionLog.PatchTurnAsync' -Params @{
    agent = $agent
    sessionId = $sessionId
    requestId = $requestId
    turn = @{
        requestId = $requestId
        queryTitle = 'Hostile C-green-P15 failsafe pending isolation green gate'
        actions = $actions
        designDecisions = @(
            'Score C-green-P15 from independently re-run P15 filter plus live MCP store and adapter source, not leftover concurrent TRX.'
            'DISAGREE: FailedSubmit does not assert root-id-named pending filename required by MCP-PLUGININT-001 P15. Independent filter Passed 24 Failed 0 Skipped 0. PLAN and PLUGININT remain done false. This DISAGREE does not authorize C-red-P16.'
        )
        tags = @('hostile-validator','PLAN-PLUGINHANDOFF-001','MCP-PLUGININT-001','C-green-P15','FR-MCP-PLUGININT-001','TR-MCP-PLUGININT-001','TEST-MCP-PLUGININT-001')
        contextList = @('docs/plans/PLAN-PLUGINHANDOFF-001.md','tests/McpServer.PluginIntegration.Tests/PluginSessionLogWorkflowAdapterTests.cs','tests/McpServer.PluginIntegration.Tests/PluginSessionLogWorkflowAdapter.cs','docs/receipts/hostile-validator-20260822T053539Z.md','docs/receipts/hostile-validator-20260822T060633Z.md','docs/receipts/hostile-validator-20260822T064147Z.md')
        interpretation = 'Hostile review Phase C-green-P15 ONLY after C-red-P15 AGREE 053539Z. Review only. Do not implement P16. Do not mark TODOs done.'
        filesModified = @('docs/receipts/hostile-validator-20260822T064147Z.md','docs/receipts/hostile-validator-20260822T064147Z.json')
        status = 'in_progress'
        response = 'Hostile C-green-P15 review DISAGREE. Receipt docs/receipts/hostile-validator-20260822T064147Z.md. Independent P15 filter Passed 24 Failed 0 Skipped 0. FailedSubmit missing root-id filename assert. PLAN-PLUGINHANDOFF-001 and MCP-PLUGININT-001 remain done false.'
    }
} -Name 'sl-patch'

Invoke-Plugin -Method 'client.SessionLog.CompleteTurnAsync' -Params @{
    agent = $agent
    sessionId = $sessionId
    requestId = $requestId
    payload = @{
        requestId = $requestId
        status = 'completed'
        response = 'Hostile C-green-P15 review DISAGREE. Receipt docs/receipts/hostile-validator-20260822T064147Z.md. Independent P15 filter Passed 24 Failed 0 Skipped 0. FailedSubmit does not assert root-id-named pending filename. PLAN-PLUGINHANDOFF-001 and MCP-PLUGININT-001 remain done false. This DISAGREE does not authorize C-red-P16 and is not plan closeout.'
        queryTitle = 'Hostile C-green-P15 failsafe pending isolation green gate'
    }
} -Name 'sl-complete'

Invoke-Plugin -Method 'client.SessionLog.QueryAsync' -Params @{
    agent = $agent
    sessionId = $sessionId
    limit = 5
} -Name 'sl-query-sid-after'

Invoke-Plugin -Method 'workflow.sessionlog.queryHistory' -Params @{
    agent = $agent
    sessionId = $sessionId
    limit = 5
    offset = 0
} -Name 'sl-query-history-after'

Write-Output 'FINISH_DONE'
