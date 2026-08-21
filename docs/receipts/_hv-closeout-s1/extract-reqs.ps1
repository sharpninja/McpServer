#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$outDir = 'F:\GitHub\McpServer\docs\receipts\_hv-closeout-s1'
$sessionDir = 'C:\Users\kingd\.grok\sessions\F%3A%5CGitHub%5CMcpServer\01a01b80-2523-7d91-8216-ebd2a0dd8879\mcp'
$files = @{
    fr = Join-Path $sessionDir 'call-d328844c-68d8-47cb-bf95-0b5d3cb46f5c-81.json'
    tr = Join-Path $sessionDir 'call-d328844c-68d8-47cb-bf95-0b5d3cb46f5c-82.json'
    test = Join-Path $sessionDir 'call-d328844c-68d8-47cb-bf95-0b5d3cb46f5c-83.json'
    mapping = Join-Path $sessionDir 'call-d328844c-68d8-47cb-bf95-0b5d3cb46f5c-84.json'
}

function Get-McpItems {
    param([string]$Path)
    $raw = Get-Content -LiteralPath $Path -Raw
    $obj = $raw | ConvertFrom-Json
    if ($obj.PSObject.Properties.Name -contains 'items') { return @($obj.items) }
    if ($obj.PSObject.Properties.Name -contains 'result') {
        $inner = $obj.result
        if ($inner -is [string]) { $inner = $inner | ConvertFrom-Json }
        if ($inner.PSObject.Properties.Name -contains 'items') { return @($inner.items) }
        return @($inner)
    }
    if ($obj.PSObject.Properties.Name -contains 'content') {
        $text = $obj.content
        if ($text -is [System.Array]) {
            $joined = ($text | ForEach-Object { $_.text }) -join ''
            $parsed = $joined | ConvertFrom-Json
            if ($parsed.PSObject.Properties.Name -contains 'items') { return @($parsed.items) }
        }
    }
    throw "Unrecognized MCP dump shape: $Path keys=$($obj.PSObject.Properties.Name -join ',')"
}

function Select-Id {
    param($Items, [string]$Id)
    foreach ($item in $Items) {
        $candidate = $item.Id
        if (-not $candidate) { $candidate = $item.FrId }
        if ($candidate -eq $Id) { return $item }
    }
    return $null
}

$frItems = Get-McpItems -Path $files.fr
$trItems = Get-McpItems -Path $files.tr
$testItems = Get-McpItems -Path $files.test
$mapItems = Get-McpItems -Path $files.mapping

$fr = Select-Id -Items $frItems -Id 'FR-MCP-TRIAGESCHEMA-001'
$tr = Select-Id -Items $trItems -Id 'TR-MCP-TRIAGESCHEMA-001'
$test = Select-Id -Items $testItems -Id 'TEST-MCP-TRIAGESCHEMA-001'
$map = Select-Id -Items $mapItems -Id 'FR-MCP-TRIAGESCHEMA-001'

function Get-AcText {
    param($Req)
    if (-not $Req) { return @() }
    $acs = @()
    if ($Req.AcceptanceCriteria) { $acs += @($Req.AcceptanceCriteria) }
    $texts = @()
    foreach ($ac in $acs) {
        $texts += [ordered]@{
            id = $ac.id
            text = $ac.text
            isSatisfied = $ac.isSatisfied
            contains20260722214500 = [bool](($ac.text + '') -match '20260722214500')
            contains20260818205751 = [bool](($ac.text + '') -match '20260818205751')
        }
    }
    return $texts
}

$result = [ordered]@{
    TimestampUtc = [DateTime]::UtcNow.ToString('yyyy-MM-ddTHH:mm:ssZ')
    FrFound = [bool]$fr
    TrFound = [bool]$tr
    TestFound = [bool]$test
    MappingFound = [bool]$map
    Fr = $fr
    Tr = $tr
    Test = $test
    Mapping = $map
    FrAc = Get-AcText $fr
    TrAc = Get-AcText $tr
    TestAc = Get-AcText $test
    FrBodyHas20260722214500 = [bool](($fr.Body + $fr.Title + '') -match '20260722214500')
    TrBodyHas20260722214500 = [bool](($tr.Body + $tr.Title + '') -match '20260722214500')
    TestConditionHas20260722214500 = [bool](($test.Condition + $test.Title + '') -match '20260722214500')
    FrBodyHasProviderIds = [bool](($fr.Body + '') -match '20260818205751')
    TrBodyHasProviderIds = [bool](($tr.Body + '') -match '20260818205751')
}

$jsonPath = Join-Path $outDir 'reqs-triageschema.json'
$result | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath $jsonPath -Encoding utf8
Write-Output $jsonPath
Write-Output ($result | ConvertTo-Json -Depth 8)
exit 0
