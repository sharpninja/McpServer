#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$outDir = 'F:\GitHub\McpServer\docs\receipts\_hv-s0-h0-reattack'
$src = 'C:\Users\kingd\.grok\sessions\F%3A%5CGitHub%5CMcpServer\01a01b3f-f652-7743-a0be-d3556deb3929\mcp\call-da460f33-ad31-4d2c-8949-4098612b2e9b-54.json'
$raw = Get-Content -LiteralPath $src -Raw
$doc = $raw | ConvertFrom-Json -Depth 80
$items = @()
if ($doc.items) { $items = @($doc.items) }
elseif ($doc.result -and $doc.result.items) { $items = @($doc.result.items) }
elseif ($doc.content) {
    $inner = $doc.content | ConvertFrom-Json -Depth 80
    if ($inner.items) { $items = @($inner.items) }
}

$want = @(
    'FR-MCP-SESSIONATTR-001'
    'FR-MCP-FAILSAFE-001'
    'FR-MCP-STRICTCOUNT-001'
    'FR-MCP-XAGENT-001'
    'FR-MCP-SESSIONEND-001'
    'FR-MCP-VERIFYWRAP-001'
    'FR-MCP-TRANSCRIPT-SEARCH-001'
    'FR-MCP-TEMPVOL-001'
)

$rows = @()
foreach ($id in $want) {
    $hit = $items | Where-Object { $_.Id -eq $id -or $_.id -eq $id } | Select-Object -First 1
    $ac = @()
    if ($hit -and $hit.AcceptanceCriteria) { $ac = @($hit.AcceptanceCriteria) }
    elseif ($hit -and $hit.acceptanceCriteria) { $ac = @($hit.acceptanceCriteria) }
    $texts = @($ac | ForEach-Object {
        if ($_.text) { [string]$_.text }
        elseif ($_.Text) { [string]$_.Text }
        else { '' }
    })
    $rows += [ordered]@{
        id = $id
        exists = [bool]$hit
        title = if ($hit) { $hit.Title } else { $null }
        status = if ($hit) { $hit.Status } else { $null }
        acCount = $ac.Count
        acNonEmpty = @($texts | Where-Object { -not [string]::IsNullOrWhiteSpace($_) }).Count
        acIds = @($ac | ForEach-Object { if ($_.id) { $_.id } elseif ($_.Id) { $_.Id } else { '' } })
        acTexts = $texts
        bodyHasCheckbox = if ($hit -and $hit.Body) { [bool]($hit.Body -match '- \[ \]') } else { $false }
        bodyLength = if ($hit -and $hit.Body) { $hit.Body.Length } else { 0 }
    }
}

$result = [ordered]@{
    source = $src
    totalItems = $items.Count
    leftover = $rows
    missing = @($rows | Where-Object { -not $_.exists } | ForEach-Object { $_.id })
    emptyAc = @($rows | Where-Object { $_.exists -and $_.acCount -eq 0 } | ForEach-Object { $_.id })
    shortAc = @($rows | Where-Object { $_.exists -and $_.acCount -ne 3 } | ForEach-Object { '{0}:{1}' -f $_.id, $_.acCount })
}
($result | ConvertTo-Json -Depth 20) | Set-Content -LiteralPath (Join-Path $outDir 'native-fr-leftover.json') -Encoding utf8
Write-Output ("FR_TOTAL={0} MISSING={1} EMPTY_AC={2} SHORT_AC={3}" -f $items.Count, ($result.missing -join ','), ($result.emptyAc -join ','), ($result.shortAc -join ','))
foreach ($r in $rows) {
    Write-Output ("{0} exists={1} ac={2} nonempty={3} checkboxBody={4}" -f $r.id, $r.exists, $r.acCount, $r.acNonEmpty, $r.bodyHasCheckbox)
}
