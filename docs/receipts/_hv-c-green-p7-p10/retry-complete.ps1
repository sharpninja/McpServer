#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'

$out = 'F:\GitHub\McpServer\docs\receipts\_hv-c-green-p7-p10'
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
$sessionId = 'GrokSubagentHostile-20260822T020204Z-c-green-p7-p10'
$requestId = 'req-20260822T020204Z-001-hostile-c-green-p7-p10'

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
    limit = 10
} -Name 'sl-query-agent'

Invoke-Plugin -Method 'workflow.sessionlog.queryHistory' -Params @{
    agent = $agent
    limit = 5
    offset = 0
} -Name 'sl-query-history'

$actions = @(
    @{ order = 1; description = 'Executed add-profile; read 18 profile markdown files'; type = 'read'; status = 'completed'; filePath = 'C:\Users\kingd\.claude\profile\PROFILE.md' }
    @{ order = 2; description = 'Independent PluginHostProcessAdapterTests Failed 0 Passed 16 Skipped 0'; type = 'test'; status = 'completed'; filePath = 'tests/McpServer.PluginIntegration.Tests/PluginHostProcessAdapterTests.cs' }
    @{ order = 3; description = 'AGREE C-green-P7-P10 because LaunchAsync calls IPluginProcessRunner.RunAsync and P11 reds are absent'; type = 'design_decision'; status = 'completed'; filePath = '' }
)

$ok = Invoke-Plugin -Method 'client.SessionLog.CompleteTurnAsync' -Params @{
    agent = $agent
    sessionId = $sessionId
    requestId = $requestId
    payload = @{
        requestId = $requestId
        status = 'completed'
        response = 'Hostile C-green-P7-P10 review AGREE. Receipt docs/receipts/hostile-validator-20260822T020756Z.md. PluginHostProcessAdapterTests Failed 0 Passed 16 Skipped 0. Full PluginIntegration.Tests Failed 0 Passed 30 Skipped 0. LaunchAsync calls IPluginProcessRunner.RunAsync. P11 reds absent. PLAN-PLUGINHANDOFF-001 and MCP-PLUGININT-001 remain done false.'
        queryTitle = 'Hostile C-green-P7-P10 plugin Session Log adapter greens'
        actions = $actions
        designDecisions = @(
            'AGREE C-green-P7-P10 from independently re-run tests, live MCP store, and adapter source.'
        )
    }
} -Name 'sl-complete-retry'

if (-not $ok) {
    Invoke-Plugin -Method 'workflow.sessionlog.completeTurn' -Params @{
        requestId = $requestId
        response = 'Hostile C-green-P7-P10 review AGREE. Receipt docs/receipts/hostile-validator-20260822T020756Z.md.'
        queryTitle = 'Hostile C-green-P7-P10 plugin Session Log adapter greens'
    } -Name 'sl-complete-workflow'
}

Invoke-Plugin -Method 'client.SessionLog.QueryAsync' -Params @{
    agent = $agent
    limit = 10
} -Name 'sl-query-agent-after'

Invoke-Plugin -Method 'workflow.sessionlog.queryHistory' -Params @{
    agent = $agent
    limit = 5
    offset = 0
} -Name 'sl-query-history-after'

Write-Output 'RETRY_COMPLETE_DONE'
