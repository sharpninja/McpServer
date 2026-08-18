#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$outDir = 'F:\GitHub\McpServer\docs\receipts'

function Get-ToolItems {
    param([string]$Path)
    $outer = Get-Content -LiteralPath $Path -Raw | ConvertFrom-Json
    $parsed = $outer.result.content[0].text | ConvertFrom-Json
    return @($parsed.items)
}

$trs = Get-ToolItems -Path (Join-Path $outDir '_hv-h4-red-req-tr.json')
$ctx = $trs | Where-Object { $_.Id -eq 'TR-MCP-PRODUCT-CTX-001' }
Write-Output ('TR_CTX_FOUND=' + [bool]$ctx)
if ($ctx) {
    Write-Output ('TR_CTX=' + ($ctx | ConvertTo-Json -Depth 10 -Compress))
}

$tests = Get-ToolItems -Path (Join-Path $outDir '_hv-h4-red-req-test.json')
$t6 = $tests | Where-Object { $_.Id -eq 'TEST-MCP-PRODUCT-006' }
Write-Output ('TEST006_FOUND=' + [bool]$t6)
if ($t6) {
    Write-Output ('TEST006=' + ($t6 | ConvertTo-Json -Depth 10 -Compress))
}

$frs = Get-ToolItems -Path (Join-Path $outDir '_hv-h4-red-req-fr.json')
$fr5 = $frs | Where-Object { $_.Id -eq 'FR-MCP-PRODUCT-005' }
Write-Output ('FR005_FOUND=' + [bool]$fr5)
if ($fr5) {
    Write-Output ('FR005=' + ($fr5 | ConvertTo-Json -Depth 10 -Compress))
}

$maps = Get-ToolItems -Path (Join-Path $outDir '_hv-h4-red-req-mapping.json')
$m5 = $maps | Where-Object { $_.FrId -eq 'FR-MCP-PRODUCT-005' }
Write-Output ('MAP005_FOUND=' + [bool]$m5)
if ($m5) {
    Write-Output ('MAP005=' + ($m5 | ConvertTo-Json -Depth 10 -Compress))
}
