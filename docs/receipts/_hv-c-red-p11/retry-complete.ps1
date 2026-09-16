#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'

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

function Invoke-Plugin {
    param([string]$Method, [hashtable]$Params, [string]$Name, [int]$TimeoutSeconds = 180)
    $outFile = Join-Path $out ($Name + '.txt')
    $errFile = Join-Path $out ($Name + '.err.txt')
    try {
        $result = & $plugin -Command Invoke -Method $Method -ParamsObject $Params -WorkspacePath 'F:\GitHub\McpServer' -CacheRoot $cacheRoot -TimeoutSeconds $TimeoutSeconds 2> $errFile
        if ($null -eq $result) { $result = '' }
        if ($result -isnot [string]) { $result = ($result | Out-String) }
        Set-Content -LiteralPath $outFile -Value $result -Encoding utf8
        Write-Output ('OK ' + $Name)
        return $true
    } catch {
        Set-Content -LiteralPath $outFile -Value ('ERROR ' + $_.Exception.ToString()) -Encoding utf8
        Write-Output ('FAIL ' + $Name + ' ' + $_.Exception.Message)
        return $false
    }
}

Invoke-Plugin -Method 'client.SessionLog.QueryAsync' -Params @{
    agent = $agent
    sessionId = $sessionId
    limit = 5
} -Name 'sl-query-sid-before-retry'

$actions = @(
    @{ order = 1; description = 'Executed add-profile; read 18 profile markdown files'; type = 'read'; status = 'completed'; filePath = 'C:\Users\kingd\.claude\profile\PROFILE.md' }
    @{ order = 2; description = 'Independent Theory_EachScenario_FailsUntilAdapterOperational Failed 8 Passed 0 Skipped 0'; type = 'test'; status = 'completed'; filePath = 'tests/McpServer.PluginIntegration.Tests/PluginSessionLogWorkflowAdapterTests.cs' }
    @{ order = 3; description = 'AGREE C-red-P11 because ExecuteCanonicalTurnAsync throws not implemented and P12/P13 greens are absent'; type = 'design_decision'; status = 'completed'; filePath = '' }
)

$ok = Invoke-Plugin -Method 'client.SessionLog.CompleteTurnAsync' -Params @{
    agent = $agent
    sessionId = $sessionId
    requestId = $requestId
    payload = @{
        requestId = $requestId
        status = 'completed'
        response = 'Hostile C-red-P11 review AGREE. Receipt docs/receipts/hostile-validator-20260822T023808Z.md. Theory_EachScenario_FailsUntilAdapterOperational Failed 8 Passed 0 Skipped 0. Full PluginIntegration.Tests Failed 8 Passed 30 Skipped 0 Total 38. ExecuteCanonicalTurnAsync throws not implemented. P12/P13 absent. PLAN-PLUGINHANDOFF-001 and MCP-PLUGININT-001 remain done false.'
        queryTitle = 'Hostile C-red-P11 plugin Session Log workflow adapter reds'
        actions = $actions
        designDecisions = @(
            'AGREE C-red-P11 from independently re-run tests, live MCP store, and adapter source.'
        )
    }
} -Name 'sl-complete-retry'

if (-not $ok) {
    Invoke-Plugin -Method 'workflow.sessionlog.completeTurn' -Params @{
        requestId = $requestId
        response = 'Hostile C-red-P11 review AGREE. Receipt docs/receipts/hostile-validator-20260822T023808Z.md.'
        queryTitle = 'Hostile C-red-P11 plugin Session Log workflow adapter reds'
    } -Name 'sl-complete-workflow'
}

if (-not $ok) {
    try {
        $completeOut = Join-Path $out 'sl-complete-command.txt'
        $completeErr = Join-Path $out 'sl-complete-command.err.txt'
        $cmdResult = & $plugin -Command CompleteTurn -Response 'Hostile C-red-P11 review AGREE. Receipt docs/receipts/hostile-validator-20260822T023808Z.md.' -WorkspacePath 'F:\GitHub\McpServer' -CacheRoot $cacheRoot -TimeoutSeconds 180 2> $completeErr
        if ($null -eq $cmdResult) { $cmdResult = '' }
        if ($cmdResult -isnot [string]) { $cmdResult = ($cmdResult | Out-String) }
        Set-Content -LiteralPath $completeOut -Value $cmdResult -Encoding utf8
        Write-Output 'OK sl-complete-command'
    } catch {
        Set-Content -LiteralPath (Join-Path $out 'sl-complete-command.txt') -Value ('ERROR ' + $_.Exception.ToString()) -Encoding utf8
        Write-Output ('FAIL sl-complete-command ' + $_.Exception.Message)
    }
}

Invoke-Plugin -Method 'client.SessionLog.QueryAsync' -Params @{
    agent = $agent
    limit = 10
} -Name 'sl-query-agent-after'

Invoke-Plugin -Method 'client.SessionLog.QueryAsync' -Params @{
    agent = $agent
    sessionId = $sessionId
    limit = 5
} -Name 'sl-query-sid-after'

Invoke-Plugin -Method 'workflow.sessionlog.queryHistory' -Params @{
    agent = $agent
    limit = 5
    offset = 0
} -Name 'sl-query-history-after'

Write-Output 'RETRY_COMPLETE_DONE'
