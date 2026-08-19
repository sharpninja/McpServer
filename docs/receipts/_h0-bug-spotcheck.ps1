#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'
$pluginRoot = 'F:\GitHub\mcpserver-grok-plugin'
$workspace = 'F:\GitHub\McpServer'
$outDir = Join-Path $workspace 'docs\receipts\_h0-hostile-raw'
$env:MCP_PLUGIN_ROOT = $pluginRoot
$env:GROK_PLUGIN_ROOT = $pluginRoot
$env:PLUGIN_AGENT_NAME = 'GrokCode'
$env:MCP_AGENT_NAME = 'GrokCode'
$env:MCP_WORKSPACE_PATH = $workspace
Set-Location -LiteralPath $workspace
$invoke = Join-Path $pluginRoot 'lib\Invoke-McpPlugin.ps1'
foreach ($id in @('BUG-TRIAGE-119','BUG-TRIAGE-110','BUG-TRIAGE-139')) {
    try {
        $r = [string](& $invoke -Command Invoke -Method 'workflow.todo.get' -Params "id: $id" -WorkspacePath $workspace)
    } catch {
        $r = [string]$_.Exception.Message
    }
    Set-Content -LiteralPath (Join-Path $outDir "30-todo-$id.txt") -Value $r -Encoding utf8
    if ($r -match 'done:\s*(\S+)') { Write-Output "$id done=$($Matches[1])" } else { Write-Output "$id parse-fail len=$($r.Length)" }
}
