#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$outDir = 'F:\GitHub\McpServer\docs\receipts\_hv-s0-h0-reattack'
$session = 'C:\Users\kingd\.grok\sessions\F%3A%5CGitHub%5CMcpServer\01a01b3f-f652-7743-a0be-d3556deb3929\mcp'
$trPath = Join-Path $session 'call-60dc6b39-02fd-4e88-84b9-594ebe75c745-57.json'
$testPath = Join-Path $session 'call-60dc6b39-02fd-4e88-84b9-594ebe75c745-58.json'
$mapPath = Join-Path $session 'call-60dc6b39-02fd-4e88-84b9-594ebe75c745-59.json'

function Get-Items {
    param([string]$Path)
    $raw = Get-Content -LiteralPath $Path -Raw
    $doc = $raw | ConvertFrom-Json -Depth 80
    if ($doc.items) { return @($doc.items) }
    if ($doc.result -and $doc.result.items) { return @($doc.result.items) }
    if ($doc.content) {
        $inner = $doc.content | ConvertFrom-Json -Depth 80
        if ($inner.items) { return @($inner.items) }
    }
    return @()
}

function Get-AcInfo {
    param($Hit)
    $ac = @()
    if ($Hit -and $Hit.AcceptanceCriteria) { $ac = @($Hit.AcceptanceCriteria) }
    elseif ($Hit -and $Hit.acceptanceCriteria) { $ac = @($Hit.acceptanceCriteria) }
    $texts = @($ac | ForEach-Object {
        if ($_.text) { [string]$_.text }
        elseif ($_.Text) { [string]$_.Text }
        else { '' }
    })
    return [ordered]@{
        acCount = $ac.Count
        acNonEmpty = @($texts | Where-Object { -not [string]::IsNullOrWhiteSpace($_) }).Count
        acIds = @($ac | ForEach-Object { if ($_.id) { $_.id } elseif ($_.Id) { $_.Id } else { '' } })
        acTexts = $texts
    }
}

$areas = @(
    'SESSIONATTR'
    'FAILSAFE'
    'STRICTCOUNT'
    'XAGENT'
    'SESSIONEND'
    'VERIFYWRAP'
    'TRANSCRIPT-SEARCH'
    'TEMPVOL'
)

$trItems = Get-Items $trPath
$testItems = Get-Items $testPath
$mapItems = Get-Items $mapPath

$trRows = @()
$testRows = @()
$mapRows = @()
foreach ($area in $areas) {
    $trId = "TR-MCP-$area-001"
    $testId = "TEST-MCP-$area-001"
    $frId = "FR-MCP-$area-001"
    $trHit = $trItems | Where-Object { $_.Id -eq $trId -or $_.id -eq $trId } | Select-Object -First 1
    $testHit = $testItems | Where-Object { $_.Id -eq $testId -or $_.id -eq $testId } | Select-Object -First 1
    $mapHit = $mapItems | Where-Object { $_.FrId -eq $frId -or $_.frId -eq $frId } | Select-Object -First 1
    $trAc = Get-AcInfo $trHit
    $testAc = Get-AcInfo $testHit
    $trBody = if ($trHit) { [string]$trHit.Body } else { '' }
    $testCond = if ($testHit) { [string]$testHit.Condition } else { '' }
    $trRows += [ordered]@{
        id = $trId
        exists = [bool]$trHit
        title = if ($trHit) { $trHit.Title } else { $null }
        acCount = $trAc.acCount
        acNonEmpty = $trAc.acNonEmpty
        acIds = $trAc.acIds
        acTexts = $trAc.acTexts
        bodyHasCheckbox = $trBody -match '- \[ \]'
        bodyLength = $trBody.Length
    }
    $testRows += [ordered]@{
        id = $testId
        exists = [bool]$testHit
        title = if ($testHit) { $testHit.Title } else { $null }
        acCount = $testAc.acCount
        acNonEmpty = $testAc.acNonEmpty
        acIds = $testAc.acIds
        acTexts = $testAc.acTexts
        conditionHasCheckbox = $testCond -match '- \[ \]'
        conditionLength = $testCond.Length
        condition = $testCond
    }
    $trIds = @()
    $testIds = @()
    if ($mapHit) {
        if ($mapHit.TrIds) { $trIds = @($mapHit.TrIds) }
        elseif ($mapHit.trIds) { $trIds = @($mapHit.trIds) }
        if ($mapHit.TestIds) { $testIds = @($mapHit.TestIds) }
        elseif ($mapHit.testIds) { $testIds = @($mapHit.testIds) }
    }
    $oneToOne = ($trIds.Count -eq 1 -and $testIds.Count -eq 1 -and $trIds[0] -eq $trId -and $testIds[0] -eq $testId)
    $mapRows += [ordered]@{
        frId = $frId
        exists = [bool]$mapHit
        trIds = $trIds
        testIds = $testIds
        oneToOne = $oneToOne
    }
}

$result = [ordered]@{
    trTotal = $trItems.Count
    testTotal = $testItems.Count
    mapTotal = $mapItems.Count
    tr = $trRows
    test = $testRows
    mapping = $mapRows
    trMissing = @($trRows | Where-Object { -not $_.exists } | ForEach-Object { $_.id })
    testMissing = @($testRows | Where-Object { -not $_.exists } | ForEach-Object { $_.id })
    mapMissing = @($mapRows | Where-Object { -not $_.exists } | ForEach-Object { $_.frId })
    trWrongAc = @($trRows | Where-Object { $_.exists -and ($_.acCount -ne 1 -or $_.acNonEmpty -ne 1) } | ForEach-Object { '{0}:{1}/{2}' -f $_.id, $_.acCount, $_.acNonEmpty })
    testWrongAc = @($testRows | Where-Object { $_.exists -and ($_.acCount -ne 1 -or $_.acNonEmpty -ne 1) } | ForEach-Object { '{0}:{1}/{2}' -f $_.id, $_.acCount, $_.acNonEmpty })
    mapNotOneToOne = @($mapRows | Where-Object { -not $_.oneToOne } | ForEach-Object { $_.frId })
}
($result | ConvertTo-Json -Depth 20) | Set-Content -LiteralPath (Join-Path $outDir 'native-tr-test-map-leftover.json') -Encoding utf8
Write-Output ("TR_TOTAL={0} TEST_TOTAL={1} MAP_TOTAL={2}" -f $trItems.Count, $testItems.Count, $mapItems.Count)
Write-Output ("TR_MISSING={0}" -f ($result.trMissing -join ','))
Write-Output ("TEST_MISSING={0}" -f ($result.testMissing -join ','))
Write-Output ("MAP_MISSING={0}" -f ($result.mapMissing -join ','))
Write-Output ("TR_WRONG_AC={0}" -f ($result.trWrongAc -join ','))
Write-Output ("TEST_WRONG_AC={0}" -f ($result.testWrongAc -join ','))
Write-Output ("MAP_NOT_1TO1={0}" -f ($result.mapNotOneToOne -join ','))
foreach ($r in $trRows) { Write-Output ("TR {0} exists={1} ac={2} nonempty={3}" -f $r.id, $r.exists, $r.acCount, $r.acNonEmpty) }
foreach ($r in $testRows) { Write-Output ("TEST {0} exists={1} ac={2} nonempty={3}" -f $r.id, $r.exists, $r.acCount, $r.acNonEmpty) }
foreach ($r in $mapRows) { Write-Output ("MAP {0} exists={1} 1to1={2} tr={3} test={4}" -f $r.frId, $r.exists, $r.oneToOne, ($r.trIds -join '|'), ($r.testIds -join '|')) }
