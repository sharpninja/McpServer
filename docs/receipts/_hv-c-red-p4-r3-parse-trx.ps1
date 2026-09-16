#Requires -Version 7.0
$ErrorActionPreference = 'Stop'
$out = 'F:\GitHub\McpServer\docs\receipts\_hv-c-red-p4-r3'

function Get-TrxSummary([string]$path) {
    if (-not (Test-Path -LiteralPath $path)) {
        return [ordered]@{ exists = $false; path = $path }
    }
    [xml]$xml = Get-Content -LiteralPath $path -Raw
    $ns = New-Object System.Xml.XmlNamespaceManager($xml.NameTable)
    $ns.AddNamespace('t', 'http://microsoft.com/schemas/VisualStudio/TeamTest/2010')
    $counters = $xml.SelectSingleNode('//t:ResultSummary/t:Counters', $ns)
    $summary = $xml.SelectSingleNode('//t:ResultSummary', $ns)
    $units = @($xml.SelectNodes('//t:UnitTestResult', $ns) | ForEach-Object {
        $msg = ''
        $outputNode = $_.SelectSingleNode('t:Output/t:ErrorInfo/t:Message', $ns)
        if ($outputNode) { $msg = [string]$outputNode.InnerText }
        [ordered]@{
            name = [string]$_.testName
            outcome = [string]$_.outcome
            duration = [string]$_.duration
            output = $msg
        }
    })
    $skippedAttr = $null
    if ($counters.HasAttribute('skipped')) { $skippedAttr = [string]$counters.skipped }
    $notExec = $null
    if ($counters.HasAttribute('notExecuted')) { $notExec = [string]$counters.notExecuted }
    [ordered]@{
        exists = $true
        path = $path
        outcome = [string]$summary.outcome
        total = [string]$counters.total
        executed = [string]$counters.executed
        passed = [string]$counters.passed
        failed = [string]$counters.failed
        skipped = $skippedAttr
        notExecuted = $notExec
        unitOutcomes = $units
    }
}

$filter = Get-TrxSummary (Join-Path $out 'catalog-filter.trx')
$all = Get-TrxSummary (Join-Path $out 'pluginintegration-all.trx')
[ordered]@{
    TimestampUtc = [DateTime]::UtcNow.ToString('o')
    CatalogFilterExitFromLog = (Select-String -LiteralPath (Join-Path $out 'dotnet-catalog-filter.log') -Pattern 'Passed!|Failed!' | Select-Object -Last 1).Line
    PluginAllExitFromLog = (Select-String -LiteralPath (Join-Path $out 'dotnet-pluginintegration-all.log') -Pattern 'Passed!|Failed!' | Select-Object -Last 1).Line
    CatalogFilterTrx = $filter
    PluginAllTrx = $all
} | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath (Join-Path $out 'trx-summary.json') -Encoding utf8
$filter | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath (Join-Path $out 'trx-parsed.json') -Encoding utf8

$catalogCs = Get-Item 'F:\GitHub\McpServer\tests\McpServer.PluginIntegration.Tests\PluginSessionLogCatalog.cs'
$catalogJson = Get-Item 'F:\GitHub\McpServer\tests\McpServer.PluginIntegration.Tests\scenarios\plugin-sessionlog-scenarios.json'
$catalogTests = Get-Item 'F:\GitHub\McpServer\tests\McpServer.PluginIntegration.Tests\PluginSessionLogCatalogTests.cs'
$nowTs = [ordered]@{
    NowUtc = [DateTime]::UtcNow.ToString('o')
    CatalogCsLastWriteTimeUtc = $catalogCs.LastWriteTimeUtc.ToString('o')
    CatalogCsLength = $catalogCs.Length
    CatalogJsonLastWriteTimeUtc = $catalogJson.LastWriteTimeUtc.ToString('o')
    CatalogTestsLastWriteTimeUtc = $catalogTests.LastWriteTimeUtc.ToString('o')
}
$nowTs | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $out 'timestamps-refresh.json') -Encoding utf8
Write-Output ($nowTs | ConvertTo-Json)
Write-Output ($filter | ConvertTo-Json -Depth 6)
Write-Output 'PARSE_DONE'
