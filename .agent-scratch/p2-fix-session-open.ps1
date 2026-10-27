$ErrorActionPreference = 'Stop'
$env:MCP_PLUGIN_ROOT = 'F:\GitHub\mcpserver-grok-plugin'
$env:GROK_PLUGIN_ROOT = 'F:\GitHub\mcpserver-grok-plugin'
$env:PLUGIN_AGENT_NAME = 'GrokCode'
$env:MCP_AGENT_NAME = 'GrokCode'
$env:MCP_PLUGIN_HOST = 'grok'
$env:MCP_WORKSPACE_PATH = 'F:\GitHub\McpServer'
Set-Location 'F:\GitHub\McpServer'
. 'F:\GitHub\mcpserver-grok-plugin\lib\marker-resolver.ps1'
$ok = Invoke-FullBootstrap -StartDir 'F:\GitHub\McpServer'
Write-Output ('FULL_BOOTSTRAP=' + $ok)
if (-not $ok) { exit 2 }
$repl = 'F:\GitHub\mcpserver-grok-plugin\lib\repl-invoke.ps1'
$utc = [DateTime]::UtcNow.ToString('yyyyMMddTHHmmssZ')
$turn = "req-$utc-p2-fix-regate-hv"
Write-Output ('TURN=' + $turn)
$begin = @"
requestId: $turn
queryTitle: Fix three P2 unit-gate blockers then one Astra HV
queryText: Diagnose and fix the three unit-gate blockers on PR 72, re-gate until ACCEPTED, then run one Astra HV round. Do not merge. Do not bulk-close SessionLife TODOs. Do not remediate Astra FAILs after the HV.
"@
& 'C:\Program Files\WindowsApps\Microsoft.PowerShell_7.6.6.0_x64__8wekyb3d8bbwe\pwsh.exe' -NoProfile -NonInteractive -File $repl -Method 'workflow.sessionlog.beginTurn' -ParamsYaml $begin
Set-Content -LiteralPath (Join-Path $env:USERPROFILE '.grok\p2-fix-turn.txt') -Value $turn -Encoding utf8
