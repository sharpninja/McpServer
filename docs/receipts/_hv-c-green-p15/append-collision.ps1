#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'
$out = 'F:\GitHub\McpServer\docs\receipts\_hv-c-green-p15'
$plugin = 'F:\GitHub\mcpserver-grok-plugin\lib\Invoke-McpPlugin.ps1'
$cacheRoot = Join-Path $out 'plugin-cache'
Set-Location -LiteralPath 'F:\GitHub\McpServer'
$env:MCP_WORKSPACE_PATH = 'F:\GitHub\McpServer'
$env:MCPSERVER_WORKSPACE_PATH = 'F:\GitHub\McpServer'
$env:PLUGIN_AGENT_NAME = 'GrokSubagentHostile'
$env:MCP_AGENT_NAME = 'GrokSubagentHostile'
$env:GROK_PLUGIN_ROOT = 'F:\GitHub\mcpserver-grok-plugin'
$env:MCP_PLUGIN_ROOT = 'F:\GitHub\mcpserver-grok-plugin'
$env:MCP_PLUGIN_HOST = 'grok'
$agent = 'GrokSubagentHostile'
$sessionId = (Get-Content -LiteralPath (Join-Path $out 'session-id.txt') -Raw).Trim()
$requestId = (Get-Content -LiteralPath (Join-Path $out 'request-id.txt') -Raw).Trim()
$now = [DateTime]::UtcNow.ToString('o')
try {
    $r = & $plugin -Command Invoke -Method 'client.SessionLog.AppendDialogAsync' -ParamsObject @{
        agent = $agent
        sessionId = $sessionId
        requestId = $requestId
        items = @(
            @{ timestamp = $now; role = 'model'; category = 'observation'; content = 'Original full suite testhost PID tree was killed at 2026-08-22T06:32:46Z by sibling collector docs/receipts/_hv-c-green-p15/run2/kill-leftover-fullsuite.ps1 (session GrokSubagentHostile-20260822T062812Z-pluginhandoff-p15green). Filter already Passed 24 Failed 0 Skipped 0 independently. Machine cleared. Restarted full suite via WMI PID 47568 under docs/receipts/_hv-c-green-p15/full-rerun. Do not treat run2 filter as this review full-suite receipt.' }
            @{ timestamp = $now; role = 'model'; category = 'decision'; content = 'Decision: withhold AGREE until this review independently finishes dotnet test tests/McpServer.PluginIntegration.Tests -c Debug Failed 0 Skipped 0. Consequence: sibling run2 filter green is corroboration only. Rejected: using implementer 86/0/0 or run2 filter as the full-suite gate.' }
        )
    } -WorkspacePath 'F:\GitHub\McpServer' -CacheRoot $cacheRoot -TimeoutSeconds 120 2>&1 | ForEach-Object { $_.ToString() }
    Set-Content -LiteralPath (Join-Path $out 'sl-dialog-collision.txt') -Value (($r | Out-String)) -Encoding utf8
    Write-Output 'OK sl-dialog-collision'
} catch {
    Set-Content -LiteralPath (Join-Path $out 'sl-dialog-collision.txt') -Value ('ERROR ' + $_.Exception.ToString()) -Encoding utf8
    Write-Output ('FAIL ' + $_.Exception.Message)
}
