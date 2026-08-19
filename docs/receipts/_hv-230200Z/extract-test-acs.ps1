Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$dump = 'C:\Users\kingd\.grok\sessions\F%3A%5CGitHub%5CMcpServer\01a01723-7f20-71e2-a5d7-18293deacd68\mcp\call-ae231e3d-944e-4621-8765-ae7cf83e718a-62.json'
$raw = Get-Content -LiteralPath $dump -Raw
$doc = $raw | ConvertFrom-Json
# MCP wrapper may nest text
$text = $null
if ($doc.PSObject.Properties.Name -contains 'result') {
    $text = $doc.result
} elseif ($doc.PSObject.Properties.Name -contains 'content') {
    $text = $doc.content
} else {
    $text = $raw
}
if ($text -is [System.Array]) {
    $text = ($text | ForEach-Object { if ($_.text) { $_.text } else { $_ } }) -join ''
}
if ($text -isnot [string]) {
    $text = $text | ConvertTo-Json -Depth 100 -Compress
}
$payload = $text | ConvertFrom-Json
$wanted = @(
    'TEST-MCP-TRIAGEERR-001',
    'TEST-MCP-TRIAGESTORE-001','TEST-MCP-TRIAGESTORE-002','TEST-MCP-TRIAGESTORE-003','TEST-MCP-TRIAGESTORE-004',
    'TEST-MCP-TRIAGESTORE-005','TEST-MCP-TRIAGESTORE-006','TEST-MCP-TRIAGESTORE-007',
    'TEST-MCP-TRIAGEPLUGIN-001','TEST-MCP-TRIAGEPLUGIN-002','TEST-MCP-TRIAGEPLUGIN-003','TEST-MCP-TRIAGEPLUGIN-004','TEST-MCP-TRIAGEPLUGIN-005',
    'TEST-MCP-TRIAGESCHEMA-001','TEST-MCP-TRIAGETODO-001','TEST-MCP-TRIAGETODO-002','TEST-MCP-TRIAGEREQ-001','TEST-MCP-TRIAGEHELP-001'
)
$items = @()
if ($payload.items) { $items = @($payload.items) }
elseif ($payload.result.items) { $items = @($payload.result.items) }
$found = [ordered]@{}
foreach ($item in $items) {
    if ($wanted -contains $item.Id) {
        $acs = @()
        foreach ($ac in @($item.AcceptanceCriteria)) {
            $acs += [ordered]@{
                id = [string]$ac.id
                text = [string]$ac.text
                isSatisfied = [bool]$ac.isSatisfied
                textLen = ([string]$ac.text).Length
            }
        }
        $found[$item.Id] = [ordered]@{
            Id = $item.Id
            Condition = [string]$item.Condition
            Status = [string]$item.Status
            AcCount = $acs.Count
            AcceptanceCriteria = $acs
        }
    }
}
$out = [ordered]@{
    wantedCount = $wanted.Count
    foundCount = $found.Count
    missing = @($wanted | Where-Object { -not $found.Contains($_) })
    items = $found
}
$jsonPath = 'F:\GitHub\McpServer\docs\receipts\_hv-230200Z\test-ac.json'
$out | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $jsonPath -Encoding utf8
Write-Output ('FOUND=' + $found.Count + ' MISSING=' + ($out.missing -join ','))
foreach ($id in $wanted) {
    if ($found.Contains($id)) {
        $ac1 = $found[$id].AcceptanceCriteria | Select-Object -First 1
        Write-Output ('--- ' + $id + ' status=' + $found[$id].Status + ' acCount=' + $found[$id].AcCount + ' ac1Len=' + $ac1.textLen)
        Write-Output ('AC1=' + $ac1.text)
    } else {
        Write-Output ('--- ' + $id + ' MISSING')
    }
}
