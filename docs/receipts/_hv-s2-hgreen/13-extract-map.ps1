#Requires -Version 7.0
[CmdletBinding()]
param()
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$src = 'C:\Users\kingd\.grok\sessions\F%3A%5CGitHub%5CMcpServer\01a01b91-0223-70b3-b29a-4a19fe36952b\mcp\call-ba74373d-d68f-435a-8343-51763fe2c513-132.json'
$out = 'F:\GitHub\McpServer\docs\receipts\_hv-s2-hgreen\13-extract-map.json'
$want = @(
    'FR-MCP-STRICTCOUNT-001','FR-MCP-FAILSAFE-001','FR-MCP-SESSIONEND-001','FR-MCP-XAGENT-001','FR-MCP-VERIFYWRAP-001',
    'FR-MCP-TRIAGEPLUGIN-001','FR-MCP-TRIAGEERR-001'
)

$raw = Get-Content -LiteralPath $src -Raw
$doc = $raw | ConvertFrom-Json
$payload = $doc
if ($doc.PSObject.Properties.Name -contains 'result') { $payload = $doc.result }
$items = @()
if ($payload.PSObject.Properties.Name -contains 'items') { $items = @($payload.items) }
elseif ($payload.PSObject.Properties.Name -contains 'content') {
    $text = [string]$payload.content[0].text
    $inner = $text | ConvertFrom-Json
    if ($inner.PSObject.Properties.Name -contains 'items') { $items = @($inner.items) }
}

$found = @()
foreach ($item in $items) {
    $id = [string]$item.FrId
    if ($want -contains $id) {
        $found += [ordered]@{
            FrId = $id
            TrIds = @($item.TrIds)
            TestIds = @($item.TestIds)
        }
    }
}

$obj = [ordered]@{
    TimestampUtc = [datetime]::UtcNow.ToString('yyyy-MM-ddTHH:mm:ssZ')
    ItemCount = $items.Count
    FoundFrIds = @($found | ForEach-Object { $_.FrId })
    Missing = @($want | Where-Object { $_ -notin @($found | ForEach-Object { $_.FrId }) })
    Found = $found
}
$obj | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $out -Encoding utf8
Write-Output ("WROTE {0} maps={1} found={2} missing={3}" -f $out, $items.Count, $found.Count, ($obj.Missing -join ','))
