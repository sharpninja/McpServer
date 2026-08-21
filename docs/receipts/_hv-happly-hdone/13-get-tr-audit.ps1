#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$workspace = 'F:\GitHub\McpServer'
$pluginRoot = 'F:\GitHub\mcpserver-grok-plugin'
$invoke = Join-Path $pluginRoot 'lib\Invoke-McpPlugin.ps1'
$outDir = Join-Path $workspace 'docs\receipts\_hv-happly-hdone'
$cacheRoot = Join-Path $outDir 'plugin-cache'
$env:PLUGIN_AGENT_NAME = 'GrokCode'
$env:MCP_AGENT_NAME = 'GrokCode'
$env:MCP_PLUGIN_HOST = 'grok'
$env:MCP_PLUGIN_ROOT = $pluginRoot
$env:MCP_WORKSPACE_PATH = $workspace
Set-Location $workspace
$raw = & pwsh.exe -NoProfile -NonInteractive -File $invoke -Command Invoke -Method 'workflow.todo.get' -Params 'id: TR-AUDIT-001' -WorkspacePath $workspace -PluginRoot $pluginRoot -CacheRoot $cacheRoot -TimeoutSeconds 90 2>&1 | Out-String
Set-Content -LiteralPath (Join-Path $outDir '13-todo-get-TR-AUDIT-001.txt') -Value $raw -Encoding utf8
Write-Output ('EXIT=' + $LASTEXITCODE)
Write-Output ('HAS_ORPHAN=' + $raw.Contains('OrphanReason'))
Write-Output ('HAS_DATE=' + $raw.Contains('2026-08-20T101500Z'))
Write-Output ('HAS_DONE_FALSE=' + [bool]($raw -match '(?im)^\s+done:\s*false'))
