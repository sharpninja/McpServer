#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$outDir = 'F:\GitHub\McpServer\docs\receipts\_hv-closeout-s1'
$sessionDir = 'C:\Users\kingd\.grok\sessions\F%3A%5CGitHub%5CMcpServer\01a01b80-2523-7d91-8216-ebd2a0dd8879\mcp'

function Read-Dump([string]$Name) {
    $path = Join-Path $sessionDir $Name
    return (Get-Content -LiteralPath $path -Raw | ConvertFrom-Json)
}

function Get-Items($obj) {
    $list = [System.Collections.Generic.List[object]]::new()
    if ($null -eq $obj) { return $list }
    $names = @($obj.PSObject.Properties.Name)
    $items = $null
    if ($names -contains 'items') { $items = $obj.items }
    elseif ($names -contains 'result') {
        $inner = $obj.result
        if ($inner -is [string]) { $inner = $inner | ConvertFrom-Json }
        if ($inner.PSObject.Properties.Name -contains 'items') { $items = $inner.items }
        else { $items = $inner }
    }
    else { $items = $obj }
    foreach ($i in @($items)) {
        if ($null -ne $i) { $list.Add($i) }
    }
    return $list
}

function Find-ById($items, [string]$id) {
    foreach ($item in $items) {
        $names = @($item.PSObject.Properties.Name)
        $candidate = $null
        if ($names -contains 'Id') { $candidate = [string]$item.Id }
        elseif ($names -contains 'id') { $candidate = [string]$item.id }
        elseif ($names -contains 'FrId') { $candidate = [string]$item.FrId }
        if ($candidate -eq $id) { return $item }
    }
    return $null
}

function Get-AcList($req) {
    $list = [System.Collections.Generic.List[object]]::new()
    if ($null -eq $req) { return $list }
    $names = @($req.PSObject.Properties.Name)
    if ($names -notcontains 'AcceptanceCriteria') { return $list }
    $acs = $req.AcceptanceCriteria
    if ($null -eq $acs) { return $list }
    foreach ($ac in @($acs)) {
        if ($null -eq $ac) { continue }
        $text = [string]$ac.text
        $list.Add([ordered]@{
            id = [string]$ac.id
            text = $text
            isSatisfied = $ac.isSatisfied
            contains20260722214500 = $text.Contains('20260722214500')
            contains20260818205751 = $text.Contains('20260818205751')
        })
    }
    return $list
}

$frDump = Read-Dump 'call-d328844c-68d8-47cb-bf95-0b5d3cb46f5c-81.json'
$trDump = Read-Dump 'call-d328844c-68d8-47cb-bf95-0b5d3cb46f5c-82.json'
$testDump = Read-Dump 'call-d328844c-68d8-47cb-bf95-0b5d3cb46f5c-83.json'
$mapDump = Read-Dump 'call-d328844c-68d8-47cb-bf95-0b5d3cb46f5c-84.json'

$frItems = Get-Items $frDump
$trItems = Get-Items $trDump
$testItems = Get-Items $testDump
$mapItems = Get-Items $mapDump

$fr = Find-ById $frItems 'FR-MCP-TRIAGESCHEMA-001'
$tr = Find-ById $trItems 'TR-MCP-TRIAGESCHEMA-001'
$test = Find-ById $testItems 'TEST-MCP-TRIAGESCHEMA-001'
$map = Find-ById $mapItems 'FR-MCP-TRIAGESCHEMA-001'

$frAc = Get-AcList $fr
$trAc = Get-AcList $tr
$testAc = Get-AcList $test

$frBody = if ($null -ne $fr) { [string]$fr.Body } else { '' }
$trBody = if ($null -ne $tr) { [string]$tr.Body } else { '' }
$testCond = if ($null -ne $test) { [string]$test.Condition } else { '' }

$frHasOld = $frBody.Contains('20260722214500')
foreach ($ac in $frAc) { if ($ac.contains20260722214500) { $frHasOld = $true } }
$trHasOld = $trBody.Contains('20260722214500')
foreach ($ac in $trAc) { if ($ac.contains20260722214500) { $trHasOld = $true } }
$testHasOld = $testCond.Contains('20260722214500')
foreach ($ac in $testAc) { if ($ac.contains20260722214500) { $testHasOld = $true } }

$result = [ordered]@{
    TimestampUtc = [DateTime]::UtcNow.ToString('yyyy-MM-ddTHH:mm:ssZ')
    FrDumpTopKeys = @($frDump.PSObject.Properties.Name)
    FrItemCount = $frItems.Count
    TrItemCount = $trItems.Count
    TestItemCount = $testItems.Count
    MapItemCount = $mapItems.Count
    FrFound = [bool]($null -ne $fr)
    TrFound = [bool]($null -ne $tr)
    TestFound = [bool]($null -ne $test)
    MappingFound = [bool]($null -ne $map)
    FrTitle = if ($null -ne $fr) { [string]$fr.Title } else { $null }
    FrStatus = if ($null -ne $fr) { [string]$fr.Status } else { $null }
    FrBody = $frBody
    FrAc = @($frAc)
    TrTitle = if ($null -ne $tr) { [string]$tr.Title } else { $null }
    TrStatus = if ($null -ne $tr) { [string]$tr.Status } else { $null }
    TrBody = $trBody
    TrAc = @($trAc)
    TestTitle = if ($null -ne $test) { [string]$test.Title } else { $null }
    TestCondition = $testCond
    TestAc = @($testAc)
    MappingFrId = if ($null -ne $map) { [string]$map.FrId } else { $null }
    MappingTrIds = if ($null -ne $map) { @($map.TrIds) } else { @() }
    MappingTestIds = if ($null -ne $map) { @($map.TestIds) } else { @() }
    FrHas20260722214500 = $frHasOld
    TrHas20260722214500 = $trHasOld
    TestHas20260722214500 = $testHasOld
    FrHas20260818205751 = $frBody.Contains('20260818205751')
    TrHas20260818205751 = $trBody.Contains('20260818205751')
}

$jsonPath = Join-Path $outDir 'reqs-triageschema.json'
$result | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath $jsonPath -Encoding utf8
Write-Output $jsonPath
Write-Output ($result | ConvertTo-Json -Depth 10)
exit 0
