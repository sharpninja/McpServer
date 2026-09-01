#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$workspace = 'F:\GitHub\McpServer'
$outDir = 'F:\GitHub\McpServer\docs\receipts'
Set-Location $workspace

$utc = [DateTime]::UtcNow.ToString('yyyyMMddTHHmmssZ')
$sessionId = "GrokCode-$utc-h5-done-rerun-products"
$requestId = "req-$utc-001-hostile-h5-done-rerun"
$nonce = 'h5rr' + [guid]::NewGuid().ToString('N')
$idsPath = Join-Path $outDir '_hv-h5-rerun-ids.txt'

@(
    "UTC=$utc"
    "SESSION_ID=$sessionId"
    "REQUEST_ID=$requestId"
    "NONCE=$nonce"
) | Set-Content -LiteralPath $idsPath -Encoding utf8

Write-Output "UTC=$utc"
Write-Output "SESSION_ID=$sessionId"
Write-Output "REQUEST_ID=$requestId"
Write-Output "NONCE=$nonce"

# Marker signature
$markerResolver = 'F:\GitHub\mcpserver-grok-plugin\lib\marker-resolver.ps1'
$sigOk = $false
if (Test-Path -LiteralPath $markerResolver) {
    . $markerResolver
    $sigOk = Test-MarkerSignature -MarkerFile (Join-Path $workspace 'AGENTS-README-FIRST.yaml')
    Write-Output "MARKER_SIG=$sigOk"
} else {
    Write-Output "MARKER_SIG=MISSING_HELPER"
}

# Health nonce
$healthUrl = "http://PAYTON-LEGION2:7147/health?nonce=$nonce"
try {
    $health = Invoke-RestMethod -Uri $healthUrl -Method Get -TimeoutSec 30
    $health | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $outDir '_hv-h5-rerun-health.json') -Encoding utf8
    $echo = $null
    if ($health.PSObject.Properties.Name -contains 'nonce') { $echo = [string]$health.nonce }
    elseif ($health.PSObject.Properties.Name -contains 'Nonce') { $echo = [string]$health.Nonce }
    Write-Output ("HEALTH_STATUS=" + $health.status)
    Write-Output ("HEALTH_NONCE_ECHO=" + $echo)
    Write-Output ("HEALTH_NONCE_MATCH=" + ($echo -eq $nonce))
    Write-Output ("HEALTH_VERSION=" + $health.version)
} catch {
    Write-Output ("HEALTH_ERROR=" + $_.Exception.Message)
}

# Plugin version
$pluginJson = 'F:\GitHub\mcpserver-grok-plugin\.grok-plugin\plugin.json'
$pluginVer = 'F:\GitHub\mcpserver-grok-plugin\.version'
if (Test-Path -LiteralPath $pluginJson) {
    $pj = Get-Content -LiteralPath $pluginJson -Raw | ConvertFrom-Json
    Write-Output ("PLUGIN_JSON_VERSION=" + $pj.version)
}
if (Test-Path -LiteralPath $pluginVer) {
    Write-Output ("PLUGIN_VERSION_FILE=" + (Get-Content -LiteralPath $pluginVer -Raw).Trim())
}

# Tool registry
try {
    $key = (Select-String -Path (Join-Path $workspace 'AGENTS-README-FIRST.yaml') -Pattern '^apiKey:\s*(.+)$').Matches[0].Groups[1].Value.Trim()
    $headers = @{ 'X-Api-Key' = $key; 'X-Workspace-Path' = $workspace }
    $search = Invoke-RestMethod -Uri 'http://PAYTON-LEGION2:7147/mcpserver/tools/search?keyword=mcpserver-grok-plugin' -Headers $headers -TimeoutSec 30
    $search | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $outDir '_hv-h5-rerun-tool-search.json') -Encoding utf8
    $names = @()
    if ($search.items) { $names = @($search.items | ForEach-Object { $_.name }) }
    elseif ($search.Items) { $names = @($search.Items | ForEach-Object { $_.name }) }
    Write-Output ("TOOL_SEARCH_HAS_EXACT=" + ($names -contains 'mcpserver-grok-plugin'))
} catch {
    Write-Output ("TOOL_SEARCH_ERROR=" + $_.Exception.Message)
}

Write-Output 'COLLECT_DONE'
