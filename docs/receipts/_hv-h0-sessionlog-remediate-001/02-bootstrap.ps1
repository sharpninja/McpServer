#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'
$pluginRoot = 'F:\GitHub\mcpserver-grok-plugin'
$workspace = 'F:\GitHub\McpServer'
$outDir = Join-Path $workspace 'docs\receipts\_hv-h0-sessionlog-remediate-001'
$env:MCP_PLUGIN_ROOT = $pluginRoot
$env:GROK_PLUGIN_ROOT = $pluginRoot
$env:PLUGIN_AGENT_NAME = 'GrokCode'
$env:MCP_AGENT_NAME = 'GrokCode'
$env:MCP_WORKSPACE_PATH = $workspace
Set-Location -LiteralPath $workspace
$invoke = Join-Path $pluginRoot 'lib\Invoke-McpPlugin.ps1'
try {
    $output = & $invoke -Command Invoke -Method workflow.sessionlog.bootstrap -WorkspacePath $workspace -TimeoutSeconds 45 2>&1 | Out-String
} catch {
    $output = "INVOKE_EXCEPTION: $($_.Exception.Message)"
}
Set-Content -LiteralPath (Join-Path $outDir '02-bootstrap.txt') -Value $output -Encoding utf8
Write-Output $output
