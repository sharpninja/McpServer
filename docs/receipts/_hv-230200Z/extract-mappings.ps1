Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$dump = 'C:\Users\kingd\.grok\sessions\F%3A%5CGitHub%5CMcpServer\01a01723-7f20-71e2-a5d7-18293deacd68\mcp\call-b834a96f-cc11-43d9-8ec9-160d5ce09feb-67.json'
$raw = Get-Content -LiteralPath $dump -Raw
$payload = $raw | ConvertFrom-Json
$items = @()
if ($payload.items) { $items = @($payload.items) }
elseif ($payload.result) {
    $inner = $payload.result
    if ($inner -is [string]) { $inner = $inner | ConvertFrom-Json }
    if ($inner.items) { $items = @($inner.items) }
}
$wanted = @(
    'FR-MCP-TRIAGEERR-001','FR-MCP-TRIAGESTORE-001','FR-MCP-TRIAGESTORE-002','FR-MCP-TRIAGESCHEMA-001',
    'FR-MCP-TRIAGEPLUGIN-001','FR-MCP-TRIAGETODO-001','FR-MCP-TRIAGEREQ-001','FR-MCP-TRIAGEHELP-001'
)
$hits = @()
foreach ($item in $items) {
    if ($wanted -contains $item.FrId -or ([string]$item.FrId -like 'FR-MCP-TRIAGE*')) {
        $hits += [ordered]@{
            FrId = [string]$item.FrId
            TrIds = @($item.TrIds)
            TestIds = @($item.TestIds)
        }
    }
}
$outPath = 'F:\GitHub\McpServer\docs\receipts\_hv-230200Z\mappings.json'
[ordered]@{ count = $hits.Count; items = $hits } | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $outPath -Encoding utf8
Write-Output ('TRIAGE_MAPPINGS=' + $hits.Count)
foreach ($h in $hits) {
    Write-Output ($h.FrId + ' -> TR=' + ($h.TrIds -join ',') + ' TEST=' + ($h.TestIds -join ','))
}
