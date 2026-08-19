$ErrorActionPreference = 'Stop'
$path = 'C:\Users\kingd\.grok\sessions\F%3A%5CGitHub%5CMcpServer\01a0173b-e04c-73e3-9967-01456c266f4c\mcp\call-c1d9605d-8eb3-4a0d-a505-89aa30e4da1a-80.json'
$raw = Get-Content -LiteralPath $path -Raw
$payload = $raw | ConvertFrom-Json
if ($payload.PSObject.Properties.Name -contains 'result') {
    $inner = $payload.result
} else {
    $inner = $payload
}
if ($inner -is [string]) { $data = $inner | ConvertFrom-Json } else { $data = $inner }
$items = @()
if ($data.items) { $items = @($data.items) }
$want = @(
    'FR-MCP-TRIAGEERR-001',
    'FR-MCP-TRIAGEPLUGIN-001',
    'FR-MCP-TRIAGESTORE-001',
    'FR-MCP-TRIAGESTORE-002',
    'FR-MCP-TRIAGETODO-001',
    'FR-MCP-TRIAGESCHEMA-001',
    'FR-MCP-TRIAGEHELP-001',
    'FR-MCP-TRIAGEREQ-001'
)
$rows = foreach ($it in $items) {
    $id = [string]$it.FrId
    if ($want -contains $id) {
        [pscustomobject]@{
            FrId = $id
            TrIds = @($it.TrIds)
            TestIds = @($it.TestIds)
        }
    }
}
$json = $rows | ConvertTo-Json -Depth 6
$out = 'F:\GitHub\McpServer\docs\receipts\_hv-233800Z\mappings.json'
$json | Set-Content -LiteralPath $out -Encoding utf8
Write-Output $json
Write-Output ('ROWCOUNT=' + @($rows).Count)
