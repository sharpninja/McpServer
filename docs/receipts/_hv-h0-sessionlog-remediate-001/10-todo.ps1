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
$id = $args[0]
if ([string]::IsNullOrWhiteSpace($id)) { throw 'todo id required' }
$safe = $id -replace '[^A-Za-z0-9-]', '_'
try {
    $output = & $invoke -Command Invoke -Method workflow.todo.get -ParamsObject @{ id = $id } -WorkspacePath $workspace -TimeoutSeconds 60 2>&1 | Out-String
} catch {
    $output = "INVOKE_EXCEPTION: $($_.Exception.Message)"
}
Set-Content -LiteralPath (Join-Path $outDir "10-todo-$safe.txt") -Value $output -Encoding utf8
Write-Output ("DONE id=" + $id + " chars=" + $output.Length)
