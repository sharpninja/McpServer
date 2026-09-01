#Requires -Version 7.0
[CmdletBinding()]
param()
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$outDir = 'F:\GitHub\McpServer\docs\receipts\_hv-s2-testgate'
$testDump = 'C:\Users\kingd\.grok\sessions\F%3A%5CGitHub%5CMcpServer\01a01bc2-6bd3-7ac2-aeaa-4cd605dd314c\mcp\call-3746a218-e436-4959-b041-a3c71945eee7-80.json'
$trDump = 'C:\Users\kingd\.grok\sessions\F%3A%5CGitHub%5CMcpServer\01a01bc2-6bd3-7ac2-aeaa-4cd605dd314c\mcp\call-3746a218-e436-4959-b041-a3c71945eee7-81.json'

$wantedTest = @(
    'TEST-MCP-STRICTCOUNT-001',
    'TEST-MCP-FAILSAFE-001',
    'TEST-MCP-SESSIONEND-001',
    'TEST-MCP-XAGENT-001',
    'TEST-MCP-VERIFYWRAP-001',
    'TEST-MCP-TRIAGEPLUGIN-004'
)
$wantedTr = @(
    'TR-MCP-STRICTCOUNT-001',
    'TR-MCP-FAILSAFE-001',
    'TR-MCP-SESSIONEND-001',
    'TR-MCP-XAGENT-001',
    'TR-MCP-VERIFYWRAP-001',
    'TR-MCP-TRIAGEPLUGIN-001'
)

function Get-Items([string]$Path) {
    $doc = Get-Content -LiteralPath $Path -Raw | ConvertFrom-Json
    if ($doc.PSObject.Properties.Name -contains 'items') { return @($doc.items) }
    if ($doc.PSObject.Properties.Name -contains 'result' -and $doc.result.PSObject.Properties.Name -contains 'items') { return @($doc.result.items) }
    return @()
}

$testItems = Get-Items $testDump
$foundTest = @()
foreach ($id in $wantedTest) {
    $item = @($testItems | Where-Object { $_.Id -eq $id }) | Select-Object -First 1
    if ($null -eq $item) { continue }
    $foundTest += [ordered]@{
        Id = [string]$item.Id
        Title = [string]$item.Title
        Status = [string]$item.Status
        Condition = [string]$item.Condition
    }
}
$testObj = [ordered]@{
    TimestampUtc = [datetime]::UtcNow.ToString('yyyy-MM-ddTHH:mm:ssZ')
    Dump = $testDump
    ItemCount = $testItems.Count
    FoundIds = @($foundTest | ForEach-Object { $_.Id })
    Missing = @($wantedTest | Where-Object { $_ -notin @($foundTest | ForEach-Object { $_.Id }) })
    Found = $foundTest
}
$testOut = Join-Path $outDir '11-tests.json'
$testObj | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $testOut -Encoding utf8
Write-Output ("WROTE {0} items={1} found={2} missing={3}" -f $testOut, $testObj.ItemCount, $testObj.FoundIds.Count, ($testObj.Missing -join ','))

$trItems = Get-Items $trDump
$foundTr = @()
foreach ($id in $wantedTr) {
    $item = @($trItems | Where-Object { $_.Id -eq $id }) | Select-Object -First 1
    if ($null -eq $item) { continue }
    $acs = @()
    if ($item.PSObject.Properties.Name -contains 'AcceptanceCriteria' -and $null -ne $item.AcceptanceCriteria) {
        $acs = @($item.AcceptanceCriteria | ForEach-Object {
            [ordered]@{
                id = [string]$_.id
                text = [string]$_.text
                isSatisfied = [bool]$_.isSatisfied
            }
        })
    }
    $foundTr += [ordered]@{
        Id = [string]$item.Id
        Title = [string]$item.Title
        Status = [string]$item.Status
        Body = [string]$item.Body
        AcCount = $acs.Count
        AcceptanceCriteria = $acs
    }
}
$trObj = [ordered]@{
    TimestampUtc = [datetime]::UtcNow.ToString('yyyy-MM-ddTHH:mm:ssZ')
    Dump = $trDump
    ItemCount = $trItems.Count
    FoundIds = @($foundTr | ForEach-Object { $_.Id })
    Missing = @($wantedTr | Where-Object { $_ -notin @($foundTr | ForEach-Object { $_.Id }) })
    Found = $foundTr
}
$trOut = Join-Path $outDir '11-trs.json'
$trObj | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $trOut -Encoding utf8
Write-Output ("WROTE {0} items={1} found={2} missing={3}" -f $trOut, $trObj.ItemCount, $trObj.FoundIds.Count, ($trObj.Missing -join ','))
