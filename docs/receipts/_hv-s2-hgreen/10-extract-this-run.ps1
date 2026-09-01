#Requires -Version 7.0
[CmdletBinding()]
param()
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$outDir = 'F:\GitHub\McpServer\docs\receipts\_hv-s2-hgreen'
$frDump = 'C:\Users\kingd\.grok\sessions\F%3A%5CGitHub%5CMcpServer\01a01bce-1251-7640-9005-c6075b5284c0\mcp\call-7d5f5dfd-ec42-4cfc-a0dc-5d055f618527-103.json'
$trDump = 'C:\Users\kingd\.grok\sessions\F%3A%5CGitHub%5CMcpServer\01a01bce-1251-7640-9005-c6075b5284c0\mcp\call-f4c0f3c3-b4f4-4d54-90ab-82a31ef8e719-109.json'
$testDump = 'C:\Users\kingd\.grok\sessions\F%3A%5CGitHub%5CMcpServer\01a01bce-1251-7640-9005-c6075b5284c0\mcp\call-f4c0f3c3-b4f4-4d54-90ab-82a31ef8e719-110.json'
$mapDump = 'C:\Users\kingd\.grok\sessions\F%3A%5CGitHub%5CMcpServer\01a01bce-1251-7640-9005-c6075b5284c0\mcp\call-f4c0f3c3-b4f4-4d54-90ab-82a31ef8e719-111.json'

function Get-Items {
    param([string]$Path)
    $doc = Get-Content -LiteralPath $Path -Raw -Encoding utf8 | ConvertFrom-Json
    if ($doc.PSObject.Properties.Name -contains 'items') { return @($doc.items) }
    if ($doc.PSObject.Properties.Name -contains 'result' -and $doc.result.PSObject.Properties.Name -contains 'items') { return @($doc.result.items) }
    return @()
}

function Find-ById {
    param($Items, [string[]]$Wanted)
    $found = @()
    foreach ($id in $Wanted) {
        $item = @($Items | Where-Object { $_.Id -eq $id }) | Select-Object -First 1
        if ($null -eq $item) { continue }
        $acs = @()
        if ($item.PSObject.Properties.Name -contains 'AcceptanceCriteria' -and $null -ne $item.AcceptanceCriteria) {
            $acs = @($item.AcceptanceCriteria | ForEach-Object {
                [ordered]@{
                    id = $(if ($_.PSObject.Properties.Name -contains 'id') { [string]$_.id } else { $null })
                    text = $(if ($_.PSObject.Properties.Name -contains 'text') { [string]$_.text } else { [string]$_ })
                    isSatisfied = $(if ($_.PSObject.Properties.Name -contains 'isSatisfied') { [bool]$_.isSatisfied } else { $null })
                }
            })
        }
        $found += [ordered]@{
            Id = [string]$item.Id
            Title = $(if ($item.PSObject.Properties.Name -contains 'Title') { [string]$item.Title } else { $null })
            Status = $(if ($item.PSObject.Properties.Name -contains 'Status') { [string]$item.Status } else { $null })
            AcCount = $acs.Count
            AcceptanceCriteria = $acs
            Conditions = $(if ($item.PSObject.Properties.Name -contains 'Conditions') { $item.Conditions } else { $null })
        }
    }
    return $found
}

$wantedFr = @(
    'FR-MCP-STRICTCOUNT-001','FR-MCP-FAILSAFE-001','FR-MCP-SESSIONEND-001',
    'FR-MCP-XAGENT-001','FR-MCP-VERIFYWRAP-001','FR-MCP-TRIAGEPLUGIN-001'
)
$frItems = Get-Items -Path $frDump
$frFound = Find-ById -Items $frItems -Wanted $wantedFr
$frObj = [ordered]@{
    TimestampUtc = [datetime]::UtcNow.ToString('yyyy-MM-ddTHH:mm:ssZ')
    Dump = $frDump
    ItemCount = $frItems.Count
    Wanted = $wantedFr
    FoundIds = @($frFound | ForEach-Object { $_.Id })
    Missing = @($wantedFr | Where-Object { $_ -notin @($frFound | ForEach-Object { $_.Id }) })
    Found = $frFound
}
$frObj | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $outDir '11-fr.json') -Encoding utf8
Write-Output ("FR items={0} found={1} missing={2}" -f $frObj.ItemCount, $frObj.FoundIds.Count, ($frObj.Missing -join ','))

if (Test-Path -LiteralPath $trDump) {
    $wantedTr = @(
        'TR-MCP-STRICTCOUNT-001','TR-MCP-FAILSAFE-001','TR-MCP-SESSIONEND-001',
        'TR-MCP-XAGENT-001','TR-MCP-VERIFYWRAP-001','TR-MCP-TRIAGEPLUGIN-001'
    )
    $trItems = Get-Items -Path $trDump
    $trFound = Find-ById -Items $trItems -Wanted $wantedTr
    $trObj = [ordered]@{
        TimestampUtc = [datetime]::UtcNow.ToString('yyyy-MM-ddTHH:mm:ssZ')
        Dump = $trDump
        ItemCount = $trItems.Count
        Wanted = $wantedTr
        FoundIds = @($trFound | ForEach-Object { $_.Id })
        Missing = @($wantedTr | Where-Object { $_ -notin @($trFound | ForEach-Object { $_.Id }) })
        Found = $trFound
    }
    $trObj | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $outDir '11-tr.json') -Encoding utf8
    Write-Output ("TR items={0} found={1} missing={2}" -f $trObj.ItemCount, $trObj.FoundIds.Count, ($trObj.Missing -join ','))
}

if (Test-Path -LiteralPath $testDump) {
    $wantedTest = @(
        'TEST-MCP-STRICTCOUNT-001','TEST-MCP-FAILSAFE-001','TEST-MCP-SESSIONEND-001',
        'TEST-MCP-XAGENT-001','TEST-MCP-VERIFYWRAP-001','TEST-MCP-TRIAGEPLUGIN-004'
    )
    $testItems = Get-Items -Path $testDump
    $testFound = Find-ById -Items $testItems -Wanted $wantedTest
    $testObj = [ordered]@{
        TimestampUtc = [datetime]::UtcNow.ToString('yyyy-MM-ddTHH:mm:ssZ')
        Dump = $testDump
        ItemCount = $testItems.Count
        Wanted = $wantedTest
        FoundIds = @($testFound | ForEach-Object { $_.Id })
        Missing = @($wantedTest | Where-Object { $_ -notin @($testFound | ForEach-Object { $_.Id }) })
        Found = $testFound
    }
    $testObj | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $outDir '11-test.json') -Encoding utf8
    Write-Output ("TEST items={0} found={1} missing={2}" -f $testObj.ItemCount, $testObj.FoundIds.Count, ($testObj.Missing -join ','))
}

if (Test-Path -LiteralPath $mapDump) {
    $wantedMap = @(
        'FR-MCP-STRICTCOUNT-001','FR-MCP-FAILSAFE-001','FR-MCP-SESSIONEND-001',
        'FR-MCP-XAGENT-001','FR-MCP-VERIFYWRAP-001','FR-MCP-TRIAGEPLUGIN-001'
    )
    $mapItems = Get-Items -Path $mapDump
    $mapHits = @($mapItems | Where-Object {
        $fr = $null
        if ($_.PSObject.Properties.Name -contains 'FrId') { $fr = [string]$_.FrId }
        elseif ($_.PSObject.Properties.Name -contains 'frId') { $fr = [string]$_.frId }
        $wantedMap -contains $fr
    } | ForEach-Object {
        [ordered]@{
            FrId = $(if ($_.PSObject.Properties.Name -contains 'FrId') { [string]$_.FrId } else { [string]$_.frId })
            TrId = $(if ($_.PSObject.Properties.Name -contains 'TrId') { [string]$_.TrId } elseif ($_.PSObject.Properties.Name -contains 'trId') { [string]$_.trId } else { $null })
            TestId = $(if ($_.PSObject.Properties.Name -contains 'TestId') { [string]$_.TestId } elseif ($_.PSObject.Properties.Name -contains 'testId') { [string]$_.testId } else { $null })
            TrIds = $(if ($_.PSObject.Properties.Name -contains 'TrIds') { @($_.TrIds) } elseif ($_.PSObject.Properties.Name -contains 'trIds') { @($_.trIds) } else { $null })
            TestIds = $(if ($_.PSObject.Properties.Name -contains 'TestIds') { @($_.TestIds) } elseif ($_.PSObject.Properties.Name -contains 'testIds') { @($_.testIds) } else { $null })
        }
    })
    $mapObj = [ordered]@{
        TimestampUtc = [datetime]::UtcNow.ToString('yyyy-MM-ddTHH:mm:ssZ')
        Dump = $mapDump
        ItemCount = $mapItems.Count
        Hits = $mapHits
        FoundFr = @($mapHits | ForEach-Object { $_.FrId } | Select-Object -Unique)
        MissingFr = @($wantedMap | Where-Object { $_ -notin @($mapHits | ForEach-Object { $_.FrId }) })
    }
    $mapObj | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $outDir '11-map.json') -Encoding utf8
    Write-Output ("MAP items={0} hits={1} missingFr={2}" -f $mapObj.ItemCount, @($mapHits).Count, ($mapObj.MissingFr -join ','))
}
