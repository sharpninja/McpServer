#Requires -Version 7.0
[CmdletBinding()]
param()
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$src = 'C:\Users\kingd\.grok\sessions\F%3A%5CGitHub%5CMcpServer\01a01b91-0223-70b3-b29a-4a19fe36952b\mcp\call-8b6cdb79-55b5-41a8-9d19-8b0337928198-103.json'
$out = 'F:\GitHub\McpServer\docs\receipts\_hv-s2-hgreen\11-extract-tests.json'
$want = @(
    'TEST-MCP-STRICTCOUNT-001','TEST-MCP-FAILSAFE-001','TEST-MCP-SESSIONEND-001','TEST-MCP-XAGENT-001','TEST-MCP-VERIFYWRAP-001',
    'TEST-MCP-TRIAGEPLUGIN-001','TEST-MCP-TRIAGEPLUGIN-004','TEST-MCP-TRIAGEERR-001'
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
    if ($want -contains [string]$item.Id) {
        $acs = @()
        if ($item.PSObject.Properties.Name -contains 'AcceptanceCriteria' -and $item.AcceptanceCriteria) {
            foreach ($ac in @($item.AcceptanceCriteria)) {
                $acs += [ordered]@{
                    id = [string]$ac.id
                    text = [string]$ac.text
                    isSatisfied = [bool]$ac.isSatisfied
                }
            }
        }
        $found += [ordered]@{
            Id = [string]$item.Id
            Title = [string]$item.Title
            Status = [string]$item.Status
            Condition = [string]$item.Condition
            AcCount = $acs.Count
            AcceptanceCriteria = $acs
        }
    }
}

$obj = [ordered]@{
    TimestampUtc = [datetime]::UtcNow.ToString('yyyy-MM-ddTHH:mm:ssZ')
    Source = $src
    ItemCount = $items.Count
    Wanted = $want
    FoundIds = @($found | ForEach-Object { $_.Id })
    Missing = @($want | Where-Object { $_ -notin @($found | ForEach-Object { $_.Id }) })
    Found = $found
}
$obj | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $out -Encoding utf8
Write-Output ("WROTE {0} items={1} found={2} missing={3}" -f $out, $items.Count, $found.Count, ($obj.Missing -join ','))
