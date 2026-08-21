#Requires -Version 7.0
[CmdletBinding()]
param()
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$outDir = 'F:\GitHub\McpServer\docs\receipts\_hv-s2-testgate'
$mapDump = 'C:\Users\kingd\.grok\sessions\F%3A%5CGitHub%5CMcpServer\01a01bc2-6bd3-7ac2-aeaa-4cd605dd314c\mcp\call-f2cbe273-6b8b-47ae-ae07-7daafbc1199a-85.json'
$wanted = @(
    'FR-MCP-STRICTCOUNT-001',
    'FR-MCP-FAILSAFE-001',
    'FR-MCP-SESSIONEND-001',
    'FR-MCP-XAGENT-001',
    'FR-MCP-VERIFYWRAP-001',
    'FR-MCP-TRIAGEPLUGIN-001'
)
$doc = Get-Content -LiteralPath $mapDump -Raw | ConvertFrom-Json
$items = @($doc.items)
$found = @()
foreach ($id in $wanted) {
    $item = @($items | Where-Object { $_.FrId -eq $id }) | Select-Object -First 1
    if ($null -eq $item) { continue }
    $found += [ordered]@{
        FrId = [string]$item.FrId
        TrIds = @($item.TrIds)
        TestIds = @($item.TestIds)
    }
}
$obj = [ordered]@{
    TimestampUtc = [datetime]::UtcNow.ToString('yyyy-MM-ddTHH:mm:ssZ')
    Dump = $mapDump
    ItemCount = $items.Count
    FoundIds = @($found | ForEach-Object { $_.FrId })
    Missing = @($wanted | Where-Object { $_ -notin @($found | ForEach-Object { $_.FrId }) })
    Found = $found
}
$out = Join-Path $outDir '11-maps.json'
$obj | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $out -Encoding utf8
Write-Output ("WROTE {0} items={1} found={2} missing={3}" -f $out, $obj.ItemCount, $obj.FoundIds.Count, ($obj.Missing -join ','))
