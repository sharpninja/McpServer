#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'

$out = 'F:\GitHub\McpServer\docs\receipts\_hv-c-green-p15\run2'

function Get-Attr {
    param($node, [string]$name)
    if ($null -eq $node) { return $null }
    $a = $node.Attributes[$name]
    if ($null -eq $a) { return $null }
    return [string]$a.Value
}

function Get-TrxSummary {
    param([string]$TrxPath, [string]$Dest)
    if (-not (Test-Path -LiteralPath $TrxPath)) {
        [ordered]@{ exists = $false; path = $TrxPath } | ConvertTo-Json | Set-Content -LiteralPath $Dest -Encoding utf8
        return
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
        $outcomes += [ordered]@{
            name = Get-Attr $u 'testName'
            outcome = Get-Attr $u 'outcome'
            duration = Get-Attr $u 'duration'
            message = $(if ($msgNode) { [string]$msgNode.InnerText } else { $null })
        }
    }
    $p15Success = @($outcomes | Where-Object { $_.name -match 'Success_NoPendingFailsafe' })
    $p15Failed = @($outcomes | Where-Object { $_.name -match 'FailedSubmit_RetainsRootIdPending' })
    $p15Retry = @($outcomes | Where-Object { $_.name -match 'RetrySuccess_DeletesOnlyMatchingPending' })
    $p15 = @($p15Success + $p15Failed + $p15Retry)
    $p16 = @($outcomes | Where-Object { $_.name -match 'AiTheory_|P16' })
    $other = @($outcomes | Where-Object { $_.name -notmatch 'Success_NoPendingFailsafe|FailedSubmit_RetainsRootIdPending|RetrySuccess_DeletesOnlyMatchingPending' })
    [ordered]@{
        exists = $true
        path = $TrxPath
        lastWriteUtc = (Get-Item -LiteralPath $TrxPath).LastWriteTimeUtc.ToString('o')
        outcome = Get-Attr $summary 'outcome'
        total = Get-Attr $c 'total'
        executed = Get-Attr $c 'executed'
        passed = Get-Attr $c 'passed'
        failed = Get-Attr $c 'failed'
        skipped = Get-Attr $c 'skipped'
        notExecuted = Get-Attr $c 'notExecuted'
        inconclusive = Get-Attr $c 'inconclusive'
        unitCount = @($results).Count
        failedNames = @($outcomes | Where-Object { $_.outcome -eq 'Failed' } | ForEach-Object { $_.name })
        passedNames = @($outcomes | Where-Object { $_.outcome -eq 'Passed' } | ForEach-Object { $_.name })
        skippedNames = @($outcomes | Where-Object { $_.outcome -in @('NotExecuted','Skipped','Inconclusive') } | ForEach-Object { $_.name })
        p15Count = $p15.Count
        p15SuccessCount = $p15Success.Count
        p15SuccessPassed = @($p15Success | Where-Object { $_.outcome -eq 'Passed' }).Count
        p15SuccessFailed = @($p15Success | Where-Object { $_.outcome -eq 'Failed' }).Count
        p15FailedSubmitCount = $p15Failed.Count
        p15FailedSubmitPassed = @($p15Failed | Where-Object { $_.outcome -eq 'Passed' }).Count
        p15FailedSubmitFailed = @($p15Failed | Where-Object { $_.outcome -eq 'Failed' }).Count
        p15RetryCount = $p15Retry.Count
        p15RetryPassed = @($p15Retry | Where-Object { $_.outcome -eq 'Passed' }).Count
        p15RetryFailed = @($p15Retry | Where-Object { $_.outcome -eq 'Failed' }).Count
        p15Passed = @($p15 | Where-Object { $_.outcome -eq 'Passed' }).Count
        p15Failed = @($p15 | Where-Object { $_.outcome -eq 'Failed' }).Count
        p15Skipped = @($p15 | Where-Object { $_.outcome -in @('NotExecuted','Skipped','Inconclusive') }).Count
        p16Count = $p16.Count
        otherCount = $other.Count
        otherNames = @($other | ForEach-Object { $_.name })
        unitOutcomes = $outcomes
    } | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $Dest -Encoding utf8
}

$listLog = Join-Path $out 'hv-dotnet-list-tests.log'
$filterLog = Join-Path $out 'hv-dotnet-p15-filter.log'
$exitJson = Join-Path $out 'hv-dotnet-p15-filter-exit.json'

$trxPath = $null
if (Test-Path -LiteralPath $exitJson) {
    $ej = Get-Content -LiteralPath $exitJson -Raw | ConvertFrom-Json
    if ($ej.TrxFiles) {
        $trxPath = @($ej.TrxFiles) | Select-Object -First 1
    }
}
if (-not $trxPath) {
    $found = @(Get-ChildItem -LiteralPath $out -Filter '*.trx' -Recurse -ErrorAction SilentlyContinue | Sort-Object LastWriteTimeUtc -Descending)
    if ($found.Count -gt 0) { $trxPath = $found[0].FullName }
}

if ($trxPath) {
    Get-TrxSummary -TrxPath $trxPath -Dest (Join-Path $out 'filter-trx-summary.json')
}

$console = [ordered]@{}
if (Test-Path $filterLog) {
    $t = Get-Content -LiteralPath $filterLog -Raw
    $console.filterFailed = if ($t -match 'Failed:\s+(\d+)') { $Matches[1] } else { $null }
    $console.filterPassed = if ($t -match 'Passed:\s+(\d+)') { $Matches[1] } else { $null }
    $console.filterSkipped = if ($t -match 'Skipped:\s+(\d+)') { $Matches[1] } else { $null }
    $console.filterTotal = if ($t -match 'Total:\s+(\d+)') { $Matches[1] } else { $null }
    $console.filterDuration = if ($t -match 'Duration:\s+([^\r\n]+)') { $Matches[1].Trim() } else { $null }
    $console.filterFailedBang = ($t -match 'Failed!')
    $console.filterPassedBang = ($t -match 'Passed!')
    $console.p16Mentions = ([regex]::Matches($t, 'AiTheory_|P16')).Count
}
if (Test-Path $listLog) {
    $lt = Get-Content -LiteralPath $listLog
    $fqn = @($lt | Where-Object { $_ -match 'McpServer\.PluginIntegration\.Tests\.' })
    $console.listFqn = $fqn.Count
    $console.listP15Success = @($lt | Where-Object { $_ -match 'Success_NoPendingFailsafe' }).Count
    $console.listP15FailedSubmit = @($lt | Where-Object { $_ -match 'FailedSubmit_RetainsRootIdPending' }).Count
    $console.listP15Retry = @($lt | Where-Object { $_ -match 'RetrySuccess_DeletesOnlyMatchingPending' }).Count
    $console.listAiTheory = @($lt | Where-Object { $_ -match 'AiTheory_' }).Count
    $console.listP16 = @($lt | Where-Object { $_ -match 'P16' }).Count
    $console.listSkip = @($lt | Where-Object { $_ -match '\[SKIP' }).Count
}
$console | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $out 'dotnet-console-counts.json') -Encoding utf8
Write-Output 'PARSE_DONE'
Write-Output ('TRX=' + $trxPath)
