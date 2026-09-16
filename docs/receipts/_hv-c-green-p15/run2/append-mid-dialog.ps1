#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$out = 'F:\GitHub\McpServer\docs\receipts\_hv-c-green-p15\run2'
$plugin = 'F:\GitHub\mcpserver-grok-plugin\lib\Invoke-McpPlugin.ps1'
$cacheRoot = Join-Path $out 'plugin-cache'
$agent = 'GrokSubagentHostile'
$sessionId = (Get-Content -LiteralPath (Join-Path $out 'session-id.txt') -Raw).Trim()
$requestId = (Get-Content -LiteralPath (Join-Path $out 'request-id.txt') -Raw).Trim()
$now = [DateTime]::UtcNow.ToString('o')

$env:MCP_WORKSPACE_PATH = 'F:\GitHub\McpServer'
$env:MCPSERVER_WORKSPACE_PATH = 'F:\GitHub\McpServer'
$env:PLUGIN_AGENT_NAME = 'GrokSubagentHostile'
$env:MCP_AGENT_NAME = 'GrokSubagentHostile'
$env:GROK_PLUGIN_ROOT = 'F:\GitHub\mcpserver-grok-plugin'
$env:MCP_PLUGIN_ROOT = 'F:\GitHub\mcpserver-grok-plugin'
$env:MCP_PLUGIN_HOST = 'grok'

$params = @{
    agent = $agent
    sessionId = $sessionId
    requestId = $requestId
    items = @(
        @{ timestamp = $now; role = 'model'; category = 'observation'; content = 'Leftover collector tests-run PID 86108 finished P15 filter Passed 24 Failed 0 Skipped 0 Total 24 Duration 5 m 43 s, then started an out-of-scope full PluginIntegration suite. After inspect, killed leftover tree PIDs 86108,82396,70176,88088,82400,19328 so an independent unique ResultsDirectory filter could run. Hung waiter PID 32996 killed. Independent tests-run WMI PID 52344 started. Leftover TRX is corroboration only, not this review proof.' }
        @{ timestamp = $now; role = 'model'; category = 'decision'; content = 'Decision: do not treat leftover 062054Z TRX as independent proof. Consequence: required filter is run2 unique ResultsDirectory after leftover testhosts drain. Alternatives: wait 20+ min for leftover full suite or reuse leftover TRX. Rejected: parent forbids treating concurrent TRX as proof and locks C-green-P15 to the named 24-row filter.' }
    )
}

$result = & $plugin -Command Invoke -Method 'client.SessionLog.AppendDialogAsync' -ParamsObject $params -WorkspacePath 'F:\GitHub\McpServer' -CacheRoot $cacheRoot -TimeoutSeconds 180
Set-Content -LiteralPath (Join-Path $out 'sl-dialog-mid.txt') -Value ($result | Out-String) -Encoding utf8
Write-Output 'DIALOG_MID_OK'
