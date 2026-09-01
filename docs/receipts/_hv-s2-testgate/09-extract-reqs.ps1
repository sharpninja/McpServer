#Requires -Version 7.0
[CmdletBinding()]
param()
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$outDir = 'F:\GitHub\McpServer\docs\receipts\_hv-s2-testgate'
$frDump = 'C:\Users\kingd\.grok\sessions\F%3A%5CGitHub%5CMcpServer\01a01bc2-6bd3-7ac2-aeaa-4cd605dd314c\mcp\call-ac9f55ab-5063-4365-883a-561a4f06849c-78.json'

$wantedFr = @(
    'FR-MCP-STRICTCOUNT-001',
    'FR-MCP-FAILSAFE-001',
    'FR-MCP-SESSIONEND-001',
    'FR-MCP-XAGENT-001',
    'FR-MCP-VERIFYWRAP-001',
    'FR-MCP-TRIAGEPLUGIN-001'
)

$doc = Get-Content -LiteralPath $frDump -Raw | ConvertFrom-Json
$items = @()
if ($doc.PSObject.Properties.Name -contains 'items') { $items = @($doc.items) }
elseif ($doc.PSObject.Properties.Name -contains 'result') { $items = @($doc.result.items) }

$found = @()
foreach ($id in $wantedFr) {
    $item = @($items | Where-Object { $_.Id -eq $id }) | Select-Object -First 1
    if ($null -eq $item) { continue }
    $acs = @()
    if ($item.PSObject.Properties.Name -contains 'AcceptanceCriteria' -and $null -ne $item.AcceptanceCriteria) {
        $acs = @($item.AcceptanceCriteria | ForEach-Object {
            [ordered]@{
                id = [string]$_.id
                text = [string]$_.text
                isSatisfied = [bool]$_.isSatisfied
            }
        })
    }
    $found += [ordered]@{
        Id = [string]$item.Id
        Title = [string]$item.Title
        Status = [string]$item.Status
        AcCount = $acs.Count
        AcceptanceCriteria = $acs
    }
}

$obj = [ordered]@{
    TimestampUtc = [datetime]::UtcNow.ToString('yyyy-MM-ddTHH:mm:ssZ')
    Dump = $frDump
    ItemCount = $items.Count
    Wanted = $wantedFr
    FoundIds = @($found | ForEach-Object { $_.Id })
    Missing = @($wantedFr | Where-Object { $_ -notin @($found | ForEach-Object { $_.Id }) })
    Found = $found
}
$out = Join-Path $outDir '09-reqs.json'
$obj | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $out -Encoding utf8
Write-Output ("WROTE {0} items={1} found={2} missing={3}" -f $out, $obj.ItemCount, $obj.FoundIds.Count, ($obj.Missing -join ','))
