#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$outDir = 'F:\GitHub\McpServer\docs\receipts\_hv-closeout-s1'
$sessionDir = 'C:\Users\kingd\.grok\sessions\F%3A%5CGitHub%5CMcpServer\01a01b80-2523-7d91-8216-ebd2a0dd8879\mcp'

function Read-Dump([string]$Name) {
    $path = Join-Path $sessionDir $Name
    $raw = Get-Content -LiteralPath $path -Raw
    $obj = $raw | ConvertFrom-Json
    return $obj
}

function Get-Items($obj) {
    if ($null -eq $obj) { return @() }
    $names = @($obj.PSObject.Properties.Name)
    if ($names -contains 'items') { return @($obj.items) }
    if ($names -contains 'result') {
        $inner = $obj.result
        if ($inner -is [string]) { $inner = $inner | ConvertFrom-Json }
        if ($inner.PSObject.Properties.Name -contains 'items') { return @($inner.items) }
        return @($inner)
    }
    return @($obj)
}

function Find-ById($items, [string]$id) {
    foreach ($item in @($items)) {
        if ($null -eq $item) { continue }
        $names = @($item.PSObject.Properties.Name)
        $candidate = $null
        if ($names -contains 'Id') { $candidate = [string]$item.Id }
        elseif ($names -contains 'id') { $candidate = [string]$item.id }
        elseif ($names -contains 'FrId') { $candidate = [string]$item.FrId }
        if ($candidate -eq $id) { return $item }
    }
    return $null
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

function Get-Ac($req) {
    $out = @()
    if ($null -eq $req) { return $out }
    $names = @($req.PSObject.Properties.Name)
    if ($names -notcontains 'AcceptanceCriteria') { return $out }
    foreach ($ac in @($req.AcceptanceCriteria)) {
        if ($null -eq $ac) { continue }
        $text = [string]$ac.text
        $out += [ordered]@{
            id = [string]$ac.id
            text = $text
            isSatisfied = $ac.isSatisfied
            contains20260722214500 = $text.Contains('20260722214500')
            contains20260818205751 = $text.Contains('20260818205751')
        }
    }
    return $out
}

$frBody = if ($fr) { [string]$fr.Body } else { '' }
$trBody = if ($tr) { [string]$tr.Body } else { '' }
$testCond = if ($test) { [string]$test.Condition } else { '' }

$result = [ordered]@{
    TimestampUtc = [DateTime]::UtcNow.ToString('yyyy-MM-ddTHH:mm:ssZ')
    FrDumpTopKeys = @($frDump.PSObject.Properties.Name)
    FrItemCount = @($frItems).Count
    FrFound = [bool]$fr
    TrFound = [bool]$tr
    TestFound = [bool]$test
    MappingFound = [bool]$map
    FrTitle = if ($fr) { [string]$fr.Title } else { $null }
    FrStatus = if ($fr) { [string]$fr.Status } else { $null }
    FrBody = $frBody
    FrAc = Get-Ac $fr
    TrTitle = if ($tr) { [string]$tr.Title } else { $null }
    TrStatus = if ($tr) { [string]$tr.Status } else { $null }
    TrBody = $trBody
    TrAc = Get-Ac $tr
    TestTitle = if ($test) { [string]$test.Title } else { $null }
    TestCondition = $testCond
    TestAc = Get-Ac $test
    Mapping = $map
    FrHas20260722214500 = $frBody.Contains('20260722214500') -or ((Get-Ac $fr) | Where-Object { $_.contains20260722214500 }).Count -gt 0
    TrHas20260722214500 = $trBody.Contains('20260722214500') -or ((Get-Ac $tr) | Where-Object { $_.contains20260722214500 }).Count -gt 0
    TestHas20260722214500 = $testCond.Contains('20260722214500') -or ((Get-Ac $test) | Where-Object { $_.contains20260722214500 }).Count -gt 0
    FrHas20260818205751 = $frBody.Contains('20260818205751')
    TrHas20260818205751 = $trBody.Contains('20260818205751')
}

$jsonPath = Join-Path $outDir 'reqs-triageschema.json'
$result | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath $jsonPath -Encoding utf8
Write-Output $jsonPath
Write-Output ($result | ConvertTo-Json -Depth 10)
exit 0
