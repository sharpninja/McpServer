$ErrorActionPreference = 'Stop'
$path = 'C:\Users\kingd\.grok\sessions\F%3A%5CGitHub%5CMcpServer\01a0173b-e04c-73e3-9967-01456c266f4c\mcp\call-25cc3978-1c5b-46bf-b841-aecb368fbb34-47.json'
$raw = Get-Content -LiteralPath $path -Raw
$payload = $raw | ConvertFrom-Json
if ($payload.PSObject.Properties.Name -contains 'result') {
    $inner = $payload.result
} else {
    $inner = $payload
}
if ($inner -is [string]) {
    $data = $inner | ConvertFrom-Json
} else {
    $data = $inner
}
$want = @(
    'TEST-MCP-TRIAGEERR-001',
    'TEST-MCP-TRIAGESTORE-001',
    'TEST-MCP-TRIAGESTORE-002',
    'TEST-MCP-TRIAGESTORE-003',
    'TEST-MCP-TRIAGESTORE-004',
    'TEST-MCP-TRIAGESTORE-005',
    'TEST-MCP-TRIAGESTORE-006',
    'TEST-MCP-TRIAGESTORE-007',
    'TEST-MCP-TRIAGEPLUGIN-001',
    'TEST-MCP-TRIAGEPLUGIN-002',
    'TEST-MCP-TRIAGEPLUGIN-003',
    'TEST-MCP-TRIAGEPLUGIN-004',
    'TEST-MCP-TRIAGEPLUGIN-005',
    'TEST-MCP-TRIAGESCHEMA-001',
    'TEST-MCP-TRIAGETODO-001',
    'TEST-MCP-TRIAGETODO-002',
    'TEST-MCP-TRIAGEHELP-001',
    'TEST-MCP-TRIAGEREQ-001'
)
# store also uses PLUGIN-001 style in 232500Z
$want += @(
    'TRIAGEERR-001',
    'STORE-001','STORE-002','STORE-003','STORE-004','STORE-005','STORE-006','STORE-007',
    'PLUGIN-001','PLUGIN-002','PLUGIN-003','PLUGIN-004','PLUGIN-005',
    'SCHEMA-001','TODO-001','TODO-002','HELP-001','REQ-001'
)
$items = @()
if ($data.items) { $items = @($data.items) }
elseif ($data.Items) { $items = @($data.Items) }
$rows = foreach ($it in $items) {
    $id = [string]$it.Id
    if (-not $id) { $id = [string]$it.id }
    $hit = $false
    foreach ($w in $want) {
        if ($id -eq $w -or $id -like ('*' + $w) -or $id -like ('TEST-MCP-TRIAGE' + '*')) { $hit = $true; break }
    }
    if ($id -match 'TRIAGE|STORE-00|PLUGIN-00|SCHEMA-001|TODO-00[12]|HELP-001|REQ-001') { $hit = $true }
    if (-not $hit) { continue }
    $acs = @()
    if ($it.AcceptanceCriteria) { $acs = @($it.AcceptanceCriteria) }
    $ac1 = $null
    if ($acs.Count -gt 0) { $ac1 = $acs[0] }
    [pscustomobject]@{
        Id = $id
        Status = $it.Status
        AcCount = $acs.Count
        Ac1Id = $(if ($ac1) { $ac1.id } else { $null })
        Ac1Len = $(if ($ac1 -and $ac1.text) { $ac1.text.Length } else { 0 })
        Ac1Text = $(if ($ac1) { $ac1.text } else { $null })
    }
}
$rows | Sort-Object Id | ConvertTo-Json -Depth 6
$out = 'F:\GitHub\McpServer\docs\receipts\_hv-233800Z\test-ac.json'
$rows | Sort-Object Id | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $out -Encoding utf8
Write-Output ('ROWCOUNT=' + @($rows).Count)
Write-Output ('OUT=' + $out)
