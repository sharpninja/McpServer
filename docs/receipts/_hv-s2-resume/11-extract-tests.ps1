#Requires -Version 7.0
[CmdletBinding()]
param()
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$outDir = 'F:\GitHub\McpServer\docs\receipts\_hv-s2-resume'
$dump = 'C:\Users\kingd\.grok\sessions\F%3A%5CGitHub%5CMcpServer\01a01bb0-3a6a-7e33-b430-95bcd88e5f26\mcp\call-e1640c04-8902-48ec-9d46-4f6050bd4083-117.json'
$doc = (Get-Content -LiteralPath $dump -Raw) | ConvertFrom-Json
$items = @($doc.items)
$want = @(
    'TEST-MCP-STRICTCOUNT-001',
    'TEST-MCP-FAILSAFE-001',
    'TEST-MCP-SESSIONEND-001',
    'TEST-MCP-XAGENT-001',
    'TEST-MCP-VERIFYWRAP-001',
    'TEST-MCP-TRIAGEPLUGIN-004'
)
$found = @()
foreach ($it in $items) {
    $id = [string]$it.Id
    if ($want -contains $id) {
        $found += [ordered]@{
            Id = $id
            Title = [string]$it.Title
            Status = [string]$it.Status
            Condition = [string]$it.Condition
        }
    }
}
$obj = [ordered]@{
    TimestampUtc = [datetime]::UtcNow.ToString('yyyy-MM-ddTHH:mm:ssZ')
    ItemCount = @($items).Count
    FoundIds = @($found | ForEach-Object { $_.Id })
    Missing = @($want | Where-Object { @($found.Id) -notcontains $_ })
    Found = $found
}
$obj | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $outDir '11-tests.json') -Encoding utf8
Write-Output ("WROTE 11-tests.json found={0} missing={1}" -f @($found).Count, @($obj.Missing).Count)
