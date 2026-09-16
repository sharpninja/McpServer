#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$out = 'F:\GitHub\McpServer\docs\receipts\_hv-c-green-p5-p6-20260822T012151Z'
$nonce = [guid]::NewGuid().ToString('N')
$health = Invoke-RestMethod -Uri ('http://127.0.0.1:7147/health?nonce=' + $nonce) -Method Get -TimeoutSec 15
$obj = [ordered]@{
    nonceSent = $nonce
    nonceEcho = [string]$health.nonce
    status = [string]$health.status
    match = ([string]$health.nonce -eq $nonce)
    storage = [string]$health.storage
    version = [string]$health.version
}
$obj | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $out 'health-nonce-retry.json') -Encoding utf8
Write-Output ('HEALTH_MATCH=' + $obj.match)
Write-Output ('HEALTH_STATUS=' + $obj.status)
Write-Output ('STORAGE=' + $obj.storage)
Write-Output ('VERSION=' + $obj.version)

$raw = Get-Content -LiteralPath 'F:\GitHub\McpServer\AGENTS-README-FIRST.yaml' -Raw
if ($raw -match '(?m)^apiKey:\s*(.+)$') {
    $key = $Matches[1].Trim()
} else {
    throw 'no key'
}
$headers = @{ 'X-Api-Key' = $key }
$search = Invoke-RestMethod -Uri 'http://127.0.0.1:7147/mcpserver/tools/search?keyword=mcpserver-grok-plugin' -Headers $headers -Method Get -TimeoutSec 30
$search | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $out 'tool-search-grok-retry.json') -Encoding utf8
$names = @()
if ($search -is [System.Array]) {
    $names = @($search | ForEach-Object { $_.name })
} else {
    foreach ($prop in @('items', 'tools', 'results', 'value')) {
        if ($search.PSObject.Properties.Name -contains $prop -and $null -ne $search.$prop) {
            $names = @($search.$prop | ForEach-Object { $_.name })
            break
        }
    }
    if ($names.Count -eq 0 -and $search.PSObject.Properties.Name -contains 'name') {
        $names = @([string]$search.name)
    }
}
$exact = $names -contains 'mcpserver-grok-plugin'
Write-Output ('TOOL_SEARCH_EXACT=' + $exact)
Write-Output ('TOOL_SEARCH_COUNT=' + $names.Count)
Write-Output ('TOOL_SEARCH_NAMES=' + ($names -join ','))
