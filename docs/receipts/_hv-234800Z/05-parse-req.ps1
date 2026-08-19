#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$outDir = 'F:\GitHub\McpServer\docs\receipts\_hv-234800Z'

function Get-ToolPayload {
    param([string]$Path)
    $outer = Get-Content -LiteralPath $Path -Raw | ConvertFrom-Json
    $text = [string]$outer.result.content[0].text
    return ($text | ConvertFrom-Json)
}

function Get-Items {
    param($Obj)
    foreach ($name in @('items', 'Items', 'requirements', 'Requirements', 'mappings', 'Mappings')) {
        if ($Obj.PSObject.Properties.Name -contains $name -and $null -ne $Obj.$name) {
            return @($Obj.$name)
        }
    }
    if ($Obj -is [System.Array]) { return @($Obj) }
    return @()
}

$wanted = @(
    'TEST-MCP-TRIAGEERR-001','TEST-MCP-TRIAGESCHEMA-001',
    'TEST-MCP-TRIAGESTORE-001','TEST-MCP-TRIAGESTORE-002','TEST-MCP-TRIAGESTORE-003',
    'TEST-MCP-TRIAGESTORE-004','TEST-MCP-TRIAGESTORE-005','TEST-MCP-TRIAGESTORE-006','TEST-MCP-TRIAGESTORE-007',
    'TEST-MCP-TRIAGEPLUGIN-001','TEST-MCP-TRIAGEPLUGIN-002','TEST-MCP-TRIAGEPLUGIN-003','TEST-MCP-TRIAGEPLUGIN-004','TEST-MCP-TRIAGEPLUGIN-005',
    'TEST-MCP-TRIAGETODO-001','TEST-MCP-TRIAGETODO-002','TEST-MCP-TRIAGEREQ-001','TEST-MCP-TRIAGEHELP-001'
)

$testObj = Get-ToolPayload -Path (Join-Path $outDir 'req-test.json')
$items = Get-Items -Obj $testObj
$acRows = @()
foreach ($id in $wanted) {
    $hit = $null
    foreach ($item in $items) {
        $cid = $null
        if ($item.PSObject.Properties.Name -contains 'id') { $cid = [string]$item.id }
        elseif ($item.PSObject.Properties.Name -contains 'Id') { $cid = [string]$item.Id }
        if ($cid -eq $id) { $hit = $item; break }
    }
    if ($null -eq $hit) {
        $acRows += [pscustomobject]@{ id = $id; found = $false; acCount = 0; ac1Len = 0; status = $null; ac1 = $null }
        Write-Output ('TEST_MISSING ' + $id)
        continue
    }
    $acs = @()
    foreach ($n in @('acceptanceCriteria', 'AcceptanceCriteria')) {
        if ($hit.PSObject.Properties.Name -contains $n -and $null -ne $hit.$n) { $acs = @($hit.$n); break }
    }
    $ac1 = ''
    if ($acs.Count -gt 0) {
        if ($acs[0].PSObject.Properties.Name -contains 'text') { $ac1 = [string]$acs[0].text }
        elseif ($acs[0].PSObject.Properties.Name -contains 'Text') { $ac1 = [string]$acs[0].Text }
        else { $ac1 = [string]$acs[0] }
    }
    $status = $null
    if ($hit.PSObject.Properties.Name -contains 'status') { $status = [string]$hit.status }
    $acRows += [pscustomobject]@{
        id = $id
        found = $true
        acCount = $acs.Count
        ac1Len = $ac1.Length
        status = $status
        ac1 = $ac1
    }
    Write-Output ('TEST ' + $id + ' found=true acCount=' + $acs.Count + ' ac1Len=' + $ac1.Length + ' status=' + $status)
}
$acRows | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $outDir 'test-ac.json') -Encoding utf8

$mapObj = Get-ToolPayload -Path (Join-Path $outDir 'req-mapping.json')
$maps = Get-Items -Obj $mapObj
$wantedFr = @(
    'FR-MCP-TRIAGEERR-001','FR-MCP-TRIAGESTORE-001','FR-MCP-TRIAGESTORE-002',
    'FR-MCP-TRIAGESCHEMA-001','FR-MCP-TRIAGEPLUGIN-001','FR-MCP-TRIAGETODO-001',
    'FR-MCP-TRIAGEREQ-001','FR-MCP-TRIAGEHELP-001'
)
$mapRows = @()
foreach ($map in $maps) {
    $fr = $null
    foreach ($n in @('functionalRequirementId', 'FunctionalRequirementId', 'frId', 'FrId', 'fromId', 'FromId')) {
        if ($map.PSObject.Properties.Name -contains $n -and $map.$n) { $fr = [string]$map.$n; break }
    }
    if (-not $fr -and $map.PSObject.Properties.Name -contains 'fr') { $fr = [string]$map.fr }
    $keep = $false
    foreach ($id in $wantedFr) {
        $blob = ($map | ConvertTo-Json -Compress -Depth 8)
        if ($blob.Contains($id)) { $keep = $true; break }
    }
    if ($keep) {
        $mapRows += $map
        $clip = ($map | ConvertTo-Json -Compress -Depth 8)
        if ($clip.Length -gt 400) { $clip = $clip.Substring(0, 400) }
        Write-Output ('MAP ' + $clip)
    }
}
$mapRows | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $outDir 'mappings.json') -Encoding utf8
Write-Output ('MAP_ROWS=' + $mapRows.Count)
Write-Output 'PARSE_REQ_DONE'
