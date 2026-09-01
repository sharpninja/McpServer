#Requires -Version 7.0
[CmdletBinding()]
param()
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$outDir = 'F:\GitHub\McpServer\docs\receipts\_hv-s2-resume'
$dump = 'C:\Users\kingd\.grok\sessions\F%3A%5CGitHub%5CMcpServer\01a01bb0-3a6a-7e33-b430-95bcd88e5f26\mcp\call-42e9fa50-836f-4f07-aada-ced8b88ded49-112.json'
$raw = Get-Content -LiteralPath $dump -Raw
$doc = $raw | ConvertFrom-Json
$items = @()
if ($doc.items) { $items = @($doc.items) }
elseif ($doc.result.items) { $items = @($doc.result.items) }
elseif ($doc.type -eq 'fr' -and $doc.items) { $items = @($doc.items) }
else {
    # MCP wrapper may be { type, items }
    $items = @($doc.items)
}

$want = @(
    'FR-MCP-STRICTCOUNT-001',
    'FR-MCP-FAILSAFE-001',
    'FR-MCP-SESSIONEND-001',
    'FR-MCP-XAGENT-001',
    'FR-MCP-VERIFYWRAP-001',
    'FR-MCP-TRIAGEPLUGIN-001'
)
$found = @()
foreach ($it in $items) {
    $id = [string]$it.Id
    if ($want -contains $id) {
        $acs = @()
        foreach ($ac in @($it.AcceptanceCriteria)) {
            $acs += [ordered]@{
                id = [string]$ac.id
                text = [string]$ac.text
                isSatisfied = $ac.isSatisfied
            }
        }
        $found += [ordered]@{
            Id = $id
            Title = [string]$it.Title
            Status = [string]$it.Status
            AcCount = @($acs).Count
            AcceptanceCriteria = $acs
        }
    }
}
$obj = [ordered]@{
    TimestampUtc = [datetime]::UtcNow.ToString('yyyy-MM-ddTHH:mm:ssZ')
    Dump = $dump
    ItemCount = @($items).Count
    Wanted = $want
    FoundIds = @($found | ForEach-Object { $_.Id })
    Missing = @($want | Where-Object { $found.Id -notcontains $_ })
    Found = $found
}
$obj | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath (Join-Path $outDir '09-reqs.json') -Encoding utf8
Write-Output ("WROTE 09-reqs.json found={0} missing={1} totalItems={2}" -f @($found).Count, @($obj.Missing).Count, @($items).Count)
