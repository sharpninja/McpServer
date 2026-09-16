#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'

$out = 'F:\GitHub\McpServer\docs\receipts\_hv-c-red-p16\hv-self'
$plugin = 'F:\GitHub\mcpserver-grok-plugin\lib\Invoke-McpPlugin.ps1'
$cacheRoot = Join-Path $out 'plugin-cache'
$agent = 'GrokSubagentHostile'
$sessionId = 'GrokSubagentHostile-20260822T072417Z-c-red-p16'
$requestId = 'req-20260822T072417Z-001-hostile-c-red-p16'
$env:PLUGIN_AGENT_NAME = $agent
$env:MCP_AGENT_NAME = $agent
$env:GROK_PLUGIN_ROOT = 'F:\GitHub\mcpserver-grok-plugin'
$env:MCP_PLUGIN_ROOT = 'F:\GitHub\mcpserver-grok-plugin'
$env:MCP_WORKSPACE_PATH = 'F:\GitHub\McpServer'
$env:MCPSERVER_WORKSPACE_PATH = 'F:\GitHub\McpServer'
$env:GROK_WORKSPACE_PATH = 'F:\GitHub\McpServer'
$env:MCP_PLUGIN_HOST = 'grok'

function Invoke-Plugin {
    param([string]$Method, [hashtable]$Params, [string]$Name)
    $outFile = Join-Path $out ($Name + '.txt')
    $errFile = Join-Path $out ($Name + '.err.txt')
    $logFile = Join-Path $out ($Name + '.log.txt')
    try {
        $result = & $plugin -Command Invoke -Method $Method -ParamsObject $Params -WorkspacePath 'F:\GitHub\McpServer' -CacheRoot $cacheRoot -TimeoutSeconds 120 *>&1
        $text = if ($null -eq $result) { '' } else { ($result | ForEach-Object { "$_" }) -join "`n" }
        Set-Content -LiteralPath $outFile -Value $text -Encoding utf8
        Set-Content -LiteralPath $logFile -Value $text -Encoding utf8
        Write-Output ('OK ' + $Name + ' LEN=' + $text.Length)
    } catch {
        $err = $_.Exception.ToString() + "`n" + $_.ScriptStackTrace
        Set-Content -LiteralPath $outFile -Value $err -Encoding utf8
        Set-Content -LiteralPath $errFile -Value $err -Encoding utf8
        Write-Output ('FAIL ' + $Name)
        Write-Output $err
    }
}

$actions = @(
    @{ order = 1; description = 'Executed add-profile; read 18 profile markdown files'; type = 'read'; status = 'completed'; filePath = 'C:\Users\kingd\.claude\profile\PROFILE.md' }
    @{ order = 2; description = 'Health nonce nonce-hv-20260822072418-29083 echoed; plugin Test-MarkerSignature true; BeginTurnAsync turnId 42917'; type = 'read'; status = 'completed'; filePath = 'F:\GitHub\McpServer\AGENTS-README-FIRST.yaml' }
    @{ order = 3; description = 'Independent P16 filter Failed 8 Passed 0 Skipped 0; all eight host kinds fail EvaluateAsync not implemented'; type = 'test'; status = 'completed'; filePath = 'tests/McpServer.PluginIntegration.Tests/PluginSessionLogAiTheoryTests.cs' }
    @{ order = 4; description = 'Live todo_get PLAN-PLUGINHANDOFF-001 done false; MCP-PLUGININT-001 done false P16 done false'; type = 'read'; status = 'completed'; filePath = 'docs/receipts/_hv-c-red-p16/hv-self/todo-plan-final.txt' }
    @{ order = 5; description = 'Wrote hostile receipt pair 20260822T073737Z OverallVerdict AGREE FailCount 0 PassCount 17'; type = 'create'; status = 'completed'; filePath = 'docs/receipts/hostile-validator-20260822T073737Z.md' }
    @{ order = 6; description = 'AGREE C-red-P16: named theory eight PluginHostKind rows currently fail, EvaluateAsync unimplemented, P17 absent, PLAN/PLUGININT remain done false. Do not implement P17-P18. This AGREE is not plan closeout.'; type = 'design_decision'; status = 'completed'; filePath = 'docs/plans/PLAN-PLUGINHANDOFF-001.md' }
)

Invoke-Plugin -Method 'client.SessionLog.PatchTurnAsync' -Params @{
    agent = $agent
    sessionId = $sessionId
    requestId = $requestId
    turn = @{
        requestId = $requestId
        queryTitle = 'Hostile C-red-P16 AiTheory companion rows'
        actions = $actions
        designDecisions = @(
            'Score C-red-P16 from independently re-run P16 filter in unique hv-self ResultsDirectory, not sibling collector TRX.'
            'AGREE: named theory eight InlineData rows, no Skip, maps TEST-MCP-PLUGININT-001 AC3, Failed 8 Passed 0 Skipped 0. EvaluateAsync unimplemented. P17 names absent. PLAN and PLUGININT remain done false.'
        )
        tags = @('hostile-validator','PLAN-PLUGINHANDOFF-001','MCP-PLUGININT-001','C-red-P16','FR-MCP-PLUGININT-001','TR-MCP-PLUGININT-001','TEST-MCP-PLUGININT-001')
        contextList = @('docs/plans/PLAN-PLUGINHANDOFF-001.md','tests/McpServer.PluginIntegration.Tests/PluginSessionLogAiTheoryTests.cs','tests/McpServer.PluginIntegration.Tests/AiStrategyFixture.cs','docs/receipts/hostile-validator-20260822T071202Z.md','docs/receipts/hostile-validator-20260822T073737Z.md')
        interpretation = 'Hostile review Phase C-red-P16 ONLY after cited C-green-P15 AGREE. Review only. Do not implement P17-P18. Do not mark TODOs done.'
        filesModified = @('docs/receipts/hostile-validator-20260822T073737Z.md','docs/receipts/hostile-validator-20260822T073737Z.json')
        response = 'Hostile C-red-P16 review AGREE. Receipt docs/receipts/hostile-validator-20260822T073737Z.md. P16 filter Failed 8 Passed 0 Skipped 0. EvaluateAsync unimplemented. P17 names absent. PLAN-PLUGINHANDOFF-001 and MCP-PLUGININT-001 remain done false. Do not implement P17-P18. This AGREE is not plan closeout.'
    }
} -Name 'sl-patch-retry'

Invoke-Plugin -Method 'client.SessionLog.CompleteTurnAsync' -Params @{
    agent = $agent
    sessionId = $sessionId
    requestId = $requestId
    payload = @{
        requestId = $requestId
        status = 'completed'
        response = 'Hostile C-red-P16 review AGREE. Receipt docs/receipts/hostile-validator-20260822T073737Z.md. P16 filter Failed 8 Passed 0 Skipped 0. EvaluateAsync unimplemented. P17 names absent. PLAN-PLUGINHANDOFF-001 and MCP-PLUGININT-001 remain done false. Do not implement P17-P18. This AGREE is not plan closeout.'
        queryTitle = 'Hostile C-red-P16 AiTheory companion rows'
    }
} -Name 'sl-complete-retry'

try {
    $statusResult = & $plugin -Command CompleteTurn -Response 'Hostile C-red-P16 review AGREE. Receipt docs/receipts/hostile-validator-20260822T073737Z.md.' -WorkspacePath 'F:\GitHub\McpServer' -CacheRoot $cacheRoot -TimeoutSeconds 120 *>&1
    Set-Content -LiteralPath (Join-Path $out 'sl-complete-cmd.txt') -Value (($statusResult | ForEach-Object { "$_" }) -join "`n") -Encoding utf8
    Write-Output 'OK sl-complete-cmd'
} catch {
    Set-Content -LiteralPath (Join-Path $out 'sl-complete-cmd.txt') -Value $_.Exception.ToString() -Encoding utf8
    Write-Output 'FAIL sl-complete-cmd'
}

Invoke-Plugin -Method 'client.SessionLog.QueryAsync' -Params @{
    agent = $agent
    sessionId = $sessionId
    limit = 20
} -Name 'sl-query-sid-final'

Write-Output 'RETRY_COMPLETE_DONE'
