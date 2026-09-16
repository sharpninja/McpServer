#Requires -Version 7.0
[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
Set-Location -LiteralPath 'F:\GitHub\McpServer'

$utc = [DateTimeOffset]::UtcNow.ToString('yyyyMMddTHHmmssZ')
Write-Output ('UTC=' + $utc)

. .\plugins\core\lib-ps\marker-resolver.ps1
$sig = Test-MarkerSignature -MarkerFile 'F:\GitHub\McpServer\AGENTS-README-FIRST.yaml'
Write-Output ('SIGNATURE=' + $sig)

$nonce = [guid]::NewGuid().ToString('N')
$health = Invoke-RestMethod -Uri ('http://PAYTON-LEGION2:7147/health?nonce=' + $nonce)
Write-Output ('NONCE_SENT=' + $nonce)
Write-Output ('NONCE_ECHO=' + $health.nonce)
Write-Output ('NONCE_MATCH=' + ($health.nonce -eq $nonce))
Write-Output ('HEALTH_STATUS=' + $health.status)
Write-Output ('HEALTH_STORAGE=' + $health.storage)
Write-Output ('HEALTH_VERSION=' + $health.version)

$pluginVersion = (Get-Content -LiteralPath 'F:\GitHub\mcpserver-grok-plugin\.version' -Raw).Trim()
$pluginJson = Get-Content -LiteralPath 'F:\GitHub\mcpserver-grok-plugin\.claude-plugin\plugin.json' -Raw | ConvertFrom-Json
Write-Output ('PLUGIN_VERSION_FILE=' + $pluginVersion)
Write-Output ('PLUGIN_JSON_VERSION=' + $pluginJson.version)
Write-Output ('PLUGIN_JSON_NAME=' + $pluginJson.name)

Write-Output '---MCP_STATUS---'
& 'F:\GitHub\mcpserver-grok-plugin\lib\mcp-status.ps1'
