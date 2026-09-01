#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$path = 'C:\Users\kingd\.grok\sessions\F%3A%5CGitHub%5CMcpServer\01a01b4f-1f63-7bd2-ae2a-142fdf4e51df\mcp\call-be5cd97d-2c7c-44aa-aa02-eb51a6eccd00-83.json'
$raw = Get-Content -LiteralPath $path -Raw
$ids = @(
    'FR-MCP-TRIAGESCHEMA-001',
    'TR-MCP-TRIAGESCHEMA-001',
    'TEST-MCP-TRIAGESCHEMA-001',
    'FR-MCP-TRIAGE-002',
    'TR-MCP-TRIAGE-004',
    'FR-MCP-SESSIONATTR-001',
    'FR-MCP-FAILSAFE-001'
)

$hits = foreach ($id in $ids) {
    [pscustomobject]@{
        Id = $id
        Present = $raw.Contains($id)
        Count = ([regex]::Matches($raw, [regex]::Escape($id))).Count
    }
}

# Pull a small window around TRIAGESCHEMA if present
$idx = $raw.IndexOf('FR-MCP-TRIAGESCHEMA-001')
$snippet = if ($idx -ge 0) {
    $start = [Math]::Max(0, $idx - 200)
    $len = [Math]::Min(2500, $raw.Length - $start)
    $raw.Substring($start, $len)
} else { $null }

[ordered]@{
    TimestampUtc = [DateTime]::UtcNow.ToString('yyyy-MM-ddTHH:mm:ssZ')
    Source = $path
    Hits = $hits
    Snippet = $snippet
} | ConvertTo-Json -Depth 6
