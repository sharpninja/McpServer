#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'

$pluginRoot = 'C:\Users\kingd\.grok\installed-plugins\f--github-mcpserver-grok-plugin-67f1f31f'
$workspace = 'F:\GitHub\McpServer'
$outDir = Join-Path $workspace 'docs\receipts\_hv-s6-live-redeploy-20260821'
$cache = Join-Path $outDir 'plugin-cache'
$invoke = Join-Path $pluginRoot 'lib\Invoke-McpPlugin.ps1'
$env:MCP_PLUGIN_ROOT = $pluginRoot
$env:GROK_PLUGIN_ROOT = $pluginRoot
$env:PLUGIN_AGENT_NAME = 'GrokCode'
$env:MCP_WORKSPACE_PATH = $workspace
Set-Location -LiteralPath $workspace

$sessionId = 'GrokCode-20260821T103450Z-plugin-session'

$q = & $invoke -Command Invoke -Method 'client.SessionLog.QueryAsync' -ParamsObject @{
    agent = 'GrokCode'
    sessionId = $sessionId
    limit = 1
    offset = 0
} -WorkspacePath $workspace -CacheRoot $cache -TimeoutSeconds 90 2>&1 | Out-String
Set-Content -LiteralPath (Join-Path $outDir '19-query-client.txt') -Value $q -Encoding utf8
Write-Output ('queryChars=' + $q.Length)
Write-Output 'QUERY2_DONE'
