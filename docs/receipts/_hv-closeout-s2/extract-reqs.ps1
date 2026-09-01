#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$outDir = 'F:\GitHub\McpServer\docs\receipts\_hv-closeout-s2'
New-Item -ItemType Directory -Force -Path $outDir | Out-Null

$frDump = 'C:\Users\kingd\.grok\sessions\F%3A%5CGitHub%5CMcpServer\01a01ba2-bf1c-70b2-a1f8-43751c63f792\mcp\call-676cd967-eb9f-45de-86c6-d434ae78137b-69.json'
$testDump = 'C:\Users\kingd\.grok\sessions\F%3A%5CGitHub%5CMcpServer\01a01ba2-bf1c-70b2-a1f8-43751c63f792\mcp\call-380efd39-a96a-4a14-aa44-d07c2870373a-71.json'
$mapDump = 'C:\Users\kingd\.grok\sessions\F%3A%5CGitHub%5CMcpServer\01a01ba2-bf1c-70b2-a1f8-43751c63f792\mcp\call-380efd39-a96a-4a14-aa44-d07c2870373a-72.json'
$trDump = 'C:\Users\kingd\.grok\sessions\F%3A%5CGitHub%5CMcpServer\01a01ba2-bf1c-70b2-a1f8-43751c63f792\mcp\call-491fca6e-e74e-4784-85fb-0d4f65c45fcc-85.json'

function Read-Dump([string]$Path) {
    if (-not (Test-Path -LiteralPath $Path)) { return $null }
    return (Get-Content -LiteralPath $Path -Raw -Encoding utf8 | ConvertFrom-Json)
}

function Get-Items($obj) {
    $list = [System.Collections.Generic.List[object]]::new()
    if ($null -eq $obj) { return $list }
    $cursor = $obj
    if ($cursor.PSObject.Properties.Name -contains 'result') {
        $inner = $cursor.result
        if ($inner -is [string]) { $inner = $inner | ConvertFrom-Json }
        $cursor = $inner
    }
    if ($cursor.PSObject.Properties.Name -contains 'content') {
        $text = $cursor.content
        if ($text -is [System.Array]) {
            $piece = $text | Where-Object { $_.type -eq 'text' } | Select-Object -First 1
            if ($null -ne $piece) { $cursor = $piece.text | ConvertFrom-Json }
        }
        elseif ($text -is [string]) { $cursor = $text | ConvertFrom-Json }
    }
    $items = $null
    if ($cursor.PSObject.Properties.Name -contains 'items') { $items = $cursor.items }
    else { $items = $cursor }
    foreach ($i in @($items)) {
        if ($null -ne $i) { $list.Add($i) }
    }
    return $list
}

function Find-ById($items, [string]$id, [string[]]$keys) {
    foreach ($item in $items) {
        $names = @($item.PSObject.Properties.Name)
        foreach ($key in $keys) {
            if ($names -contains $key -and [string]$item.$key -eq $id) { return $item }
        }
    }
    return $null
}

function Get-AcList($req) {
    $list = [System.Collections.Generic.List[object]]::new()
    if ($null -eq $req) { return $list }
    $names = @($req.PSObject.Properties.Name)
    if ($names -notcontains 'AcceptanceCriteria') { return $list }
    foreach ($ac in @($req.AcceptanceCriteria)) {
        if ($null -eq $ac) { continue }
        $text = [string]$ac.text
        $list.Add([ordered]@{
            id = [string]$ac.id
            text = $text
            isSatisfied = $ac.isSatisfied
            contains20260722214500 = $text.Contains('20260722214500')
            contains20260818205751 = $text.Contains('20260818205751')
            contains20260818205807 = $text.Contains('20260818205807')
            contains20260818205822 = $text.Contains('20260818205822')
        })
    }
    return $list
}

$frItems = Get-Items (Read-Dump $frDump)
$testItems = Get-Items (Read-Dump $testDump)
$mapItems = Get-Items (Read-Dump $mapDump)
$trItems = if (Test-Path -LiteralPath $trDump) { Get-Items (Read-Dump $trDump) } else { @() }

$fr = Find-ById $frItems 'FR-MCP-TRIAGESCHEMA-001' @('Id','id')
$tr = Find-ById $trItems 'TR-MCP-TRIAGESCHEMA-001' @('Id','id')
$test = Find-ById $testItems 'TEST-MCP-TRIAGESCHEMA-001' @('Id','id')
$map = Find-ById $mapItems 'FR-MCP-TRIAGESCHEMA-001' @('FrId','frId','Id','id')

$frAc = Get-AcList $fr
$trAc = Get-AcList $tr
$testAc = Get-AcList $test

$frBody = if ($null -ne $fr) { [string]$fr.Body } else { '' }
$trBody = if ($null -ne $tr) { [string]$tr.Body } else { '' }
$testCond = if ($null -ne $test) { [string]$test.Condition } else { '' }
$testNotes = if ($null -ne $test) { [string]$test.Notes } else { '' }

function Test-HasOld($text, $acs) {
    $hit = $text.Contains('20260722214500')
    foreach ($ac in $acs) { if ($ac.contains20260722214500) { $hit = $true } }
    return $hit
}

$frAcArr = @($frAc)
$trAcArr = @($trAc)
$testAcArr = @($testAc)
$frAc1 = @($frAcArr | Where-Object { $_.id -eq 'ac-1' -or $_.id -eq 'ac-fr-1' -or [string]$_.id -match 'ac-1$' } | Select-Object -First 1)
$trAc1 = @($trAcArr | Where-Object { $_.id -eq 'ac-1' -or $_.id -eq 'ac-tr-1' -or [string]$_.id -match 'ac-1$' } | Select-Object -First 1)
$testAc1 = @($testAcArr | Where-Object { $_.id -eq 'ac-1' -or [string]$_.id -match 'ac-1$' } | Select-Object -First 1)
if ($frAcArr.Count -gt 0 -and ($null -eq $frAc1 -or $frAc1.Count -eq 0)) { $frAc1 = $frAcArr[0] }
if ($trAcArr.Count -gt 0 -and ($null -eq $trAc1 -or $trAc1.Count -eq 0)) { $trAc1 = $trAcArr[0] }
if ($testAcArr.Count -gt 0 -and ($null -eq $testAc1 -or $testAc1.Count -eq 0)) { $testAc1 = $testAcArr[0] }
if ($frAc1 -is [System.Array] -and $frAc1.Count -gt 0) { $frAc1 = $frAc1[0] }
if ($trAc1 -is [System.Array] -and $trAc1.Count -gt 0) { $trAc1 = $trAc1[0] }
if ($testAc1 -is [System.Array] -and $testAc1.Count -gt 0) { $testAc1 = $testAc1[0] }

$result = [ordered]@{
    TimestampUtc = [DateTime]::UtcNow.ToString('yyyy-MM-ddTHH:mm:ssZ')
    FrDump = $frDump
    TestDump = $testDump
    MapDump = $mapDump
    TrDumpExists = Test-Path -LiteralPath $trDump
    FrItemCount = @($frItems).Count
    TrItemCount = @($trItems).Count
    TestItemCount = @($testItems).Count
    MapItemCount = @($mapItems).Count
    FrFound = [bool]($null -ne $fr)
    TrFound = [bool]($null -ne $tr)
    TestFound = [bool]($null -ne $test)
    MappingFound = [bool]($null -ne $map)
    FrTitle = if ($null -ne $fr) { [string]$fr.Title } else { $null }
    FrStatus = if ($null -ne $fr) { [string]$fr.Status } else { $null }
    FrBody = $frBody
    FrAc = $frAcArr
    FrAc1 = $frAc1
    TrTitle = if ($null -ne $tr) { [string]$tr.Title } else { $null }
    TrStatus = if ($null -ne $tr) { [string]$tr.Status } else { $null }
    TrBody = $trBody
    TrAc = $trAcArr
    TrAc1 = $trAc1
    TestTitle = if ($null -ne $test) { [string]$test.Title } else { $null }
    TestCondition = $testCond
    TestNotes = $testNotes
    TestAc = $testAcArr
    TestAc1 = $testAc1
    MappingFrId = if ($null -ne $map) { [string]$map.FrId } else { $null }
    MappingTrIds = if ($null -ne $map) { @($map.TrIds) } else { @() }
    MappingTestIds = if ($null -ne $map) { @($map.TestIds) } else { @() }
    FrHas20260722214500 = Test-HasOld $frBody $frAc
    TrHas20260722214500 = Test-HasOld $trBody $trAc
    TestHas20260722214500 = Test-HasOld ($testCond + $testNotes) $testAc
    FrBodyHas20260818205751 = $frBody.Contains('20260818205751')
    TrBodyHas20260818205751 = $trBody.Contains('20260818205751')
    FrAc1HasOld = if ($null -ne $frAc1) { [bool]$frAc1.contains20260722214500 } else { $null }
    FrAc1HasNew = if ($null -ne $frAc1) { [bool]$frAc1.contains20260818205751 } else { $null }
    TrAc1HasOld = if ($null -ne $trAc1) { [bool]$trAc1.contains20260722214500 } else { $null }
    TrAc1HasNew = if ($null -ne $trAc1) { [bool]$trAc1.contains20260818205751 } else { $null }
}

$jsonPath = Join-Path $outDir 'reqs-triageschema.json'
$result | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath $jsonPath -Encoding utf8
Write-Output $jsonPath
Write-Output ($result | ConvertTo-Json -Depth 10)
exit 0
