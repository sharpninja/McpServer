#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$out = 'F:\GitHub\McpServer\docs\receipts\_hv-c-green-p19'
$plugin = 'F:\GitHub\mcpserver-grok-plugin\lib\Invoke-McpPlugin.ps1'
$cacheRoot = Join-Path $out 'plugin-cache'
Set-Location -LiteralPath 'F:\GitHub\McpServer'
$env:MCP_WORKSPACE_PATH = 'F:\GitHub\McpServer'
$env:GROK_PLUGIN_ROOT = 'F:\GitHub\mcpserver-grok-plugin'
$env:MCP_PLUGIN_ROOT = 'F:\GitHub\mcpserver-grok-plugin'
$env:PLUGIN_AGENT_NAME = 'GrokSubagentHostile'

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

Invoke-Plugin -Method 'client.SessionLog.GetTurnAsync' -Params @{
    agent = 'GrokCode'
    sessionId = 'GrokCode-20260821T113141Z-plugin-session'
    requestId = 'req-20260822T093836Z-015-continue-p19-native-suites-green'
} -Name 'sl-get-implementer-turn'

Invoke-Plugin -Method 'workflow.sessionlog.getTurn' -Params @{
    sessionId = 'GrokCode-20260821T113141Z-plugin-session'
    requestId = 'req-20260822T093836Z-015-continue-p19-native-suites-green'
} -Name 'sl-wf-get-implementer-turn'

Invoke-Plugin -Method 'client.SessionLog.QueryAsync' -Params @{
    agent = 'GrokCode'
    sessionId = 'GrokCode-20260821T113141Z-plugin-session'
    limit = 3
} -Name 'sl-query-implementer-sid'
