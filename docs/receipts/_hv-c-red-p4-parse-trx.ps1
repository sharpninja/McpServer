#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'

$out = 'F:\GitHub\McpServer\docs\receipts\_hv-c-red-p4'

function Get-Attr {
    param($node, [string]$name)
    if ($null -eq $node) { return $null }
    $a = $node.Attributes[$name]
    if ($null -eq $a) { return $null }
    return [string]$a.Value
}

function Get-TrxSummary {
    param([string]$TrxPath)
    if (-not (Test-Path -LiteralPath $TrxPath)) {
        return [ordered]@{ exists = $false; path = $TrxPath }
    }
    [xml]$x = Get-Content -LiteralPath $TrxPath
    $ns = New-Object System.Xml.XmlNamespaceManager($x.NameTable)
    $ns.AddNamespace('t', 'http://microsoft.com/schemas/VisualStudio/TeamTest/2010')
    $summary = $x.SelectSingleNode('//t:ResultSummary', $ns)
    $c = $x.SelectSingleNode('//t:ResultSummary/t:Counters', $ns)
    $results = $x.SelectNodes('//t:UnitTestResult', $ns)
    $outcomes = @()
    foreach ($u in @($results)) {
        $msgNode = $u.SelectSingleNode('t:Output/t:ErrorInfo/t:Message', $ns)
        $stackNode = $u.SelectSingleNode('t:Output/t:ErrorInfo/t:StackTrace', $ns)
        $outcomes += [ordered]@{
            name = Get-Attr $u 'testName'
            outcome = Get-Attr $u 'outcome'
            duration = Get-Attr $u 'duration'
            message = $(if ($msgNode) { [string]$msgNode.InnerText } else { $null })
        }
    }
    return [ordered]@{
        exists = $true
        path = $TrxPath
        outcome = Get-Attr $summary 'outcome'
        total = Get-Attr $c 'total'
        executed = Get-Attr $c 'executed'
        passed = Get-Attr $c 'passed'
        failed = Get-Attr $c 'failed'
        skipped = Get-Attr $c 'skipped'
        notExecuted = Get-Attr $c 'notExecuted'
        inconclusive = Get-Attr $c 'inconclusive'
        unitCount = @($results).Count
        unitOutcomes = $outcomes
    }
}

$filterSummary = Get-TrxSummary -TrxPath (Join-Path $out 'catalog-filter.trx')
$allSummary = Get-TrxSummary -TrxPath (Join-Path $out 'pluginintegration-all.trx')

$filterLog = Get-Content -LiteralPath (Join-Path $out 'dotnet-catalog-filter.log') -Raw -ErrorAction SilentlyContinue
$allLog = Get-Content -LiteralPath (Join-Path $out 'dotnet-pluginintegration-all.log') -Raw -ErrorAction SilentlyContinue

function Get-ConsoleCounts {
    param([string]$text)
    $obj = [ordered]@{
        Failed = $null
        Passed = $null
        Skipped = $null
        Total = $null
        ExitLine = $null
    }
    if ([string]::IsNullOrWhiteSpace($text)) { return $obj }
    if ($text -match 'Failed:\s*(\d+),\s*Passed:\s*(\d+),\s*Skipped:\s*(\d+),\s*Total:\s*(\d+)') {
        $obj.Failed = [int]$Matches[1]
        $obj.Passed = [int]$Matches[2]
        $obj.Skipped = [int]$Matches[3]
        $obj.Total = [int]$Matches[4]
    }
    if ($text -match '(?m)^Failed!\s*$') { $obj.ExitLine = 'Failed!' }
    elseif ($text -match '(?m)^Passed!\s*$') { $obj.ExitLine = 'Passed!' }
    return $obj
}

[ordered]@{
    TimestampUtc = [DateTime]::UtcNow.ToString('o')
    CatalogFilterTrx = $filterSummary
    PluginAllTrx = $allSummary
    CatalogFilterConsole = Get-ConsoleCounts $filterLog
    PluginAllConsole = Get-ConsoleCounts $allLog
} | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath (Join-Path $out 'trx-parsed.json') -Encoding utf8
Write-Output 'PARSE_DONE'
