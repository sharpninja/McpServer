#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'

$out = 'F:\GitHub\McpServer\docs\receipts\_hv-c-red-p14'
$trxPath = Join-Path $out 'trx-p14-rerun\p14-filter-rerun.trx'
$logPath = Join-Path $out 'dotnet-p14-filter-rerun.log'

function Get-Attr {
    param($node, [string]$name)
    if ($null -eq $node) { return $null }
    $a = $node.Attributes[$name]
    if ($null -eq $a) { return $null }
    return [string]$a.Value
}

if (-not (Test-Path -LiteralPath $trxPath)) {
    [ordered]@{ exists = $false; path = $trxPath } | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $out 'filter-rerun-trx-summary.json') -Encoding utf8
} else {
    [xml]$x = Get-Content -LiteralPath $trxPath
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
    $p14 = @($outcomes | Where-Object { $_.name -match 'PluginRootOverride_WritesOnlyWorkspaceMcpServerAgent_PoisonEmpty' })
    $p15 = @($outcomes | Where-Object { $_.name -match 'Success_NoPendingFailsafe|FailedSubmit_RetainsRootIdPending|RetrySuccess_DeletesOnlyMatchingPending' })
    $p16 = @($outcomes | Where-Object { $_.name -match 'AiTheory_' })
    [ordered]@{
        exists = $true
        path = $trxPath
        outcome = Get-Attr $summary 'outcome'
        total = Get-Attr $c 'total'
        executed = Get-Attr $c 'executed'
        passed = Get-Attr $c 'passed'
        failed = Get-Attr $c 'failed'
        skipped = Get-Attr $c 'skipped'
        notExecuted = Get-Attr $c 'notExecuted'
        unitCount = @($results).Count
        failedNames = @($outcomes | Where-Object { $_.outcome -eq 'Failed' } | ForEach-Object { $_.name })
        passedNames = @($outcomes | Where-Object { $_.outcome -eq 'Passed' } | ForEach-Object { $_.name })
        skippedNames = @($outcomes | Where-Object { $_.outcome -in @('NotExecuted','Skipped','Inconclusive') } | ForEach-Object { $_.name })
        failedMessages = @($outcomes | Where-Object { $_.outcome -eq 'Failed' } | ForEach-Object { [ordered]@{ name = $_.name; message = $_.message } })
        p14Count = $p14.Count
        p14Failed = @($p14 | Where-Object { $_.outcome -eq 'Failed' }).Count
        p14Passed = @($p14 | Where-Object { $_.outcome -eq 'Passed' }).Count
        p14Skipped = @($p14 | Where-Object { $_.outcome -in @('NotExecuted','Skipped','Inconclusive') }).Count
        p15Count = $p15.Count
        p16Count = $p16.Count
        rejectMessageCount = @($outcomes | Where-Object { $_.message -match 'must reject PLUGIN_ROOT_OVERRIDE' }).Count
    } | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $out 'filter-rerun-trx-summary.json') -Encoding utf8
}

$console = [ordered]@{}
if (Test-Path $logPath) {
    $t = Get-Content -LiteralPath $logPath -Raw
    $console.filterFailed = if ($t -match 'Failed:\s+(\d+)') { $Matches[1] } else { $null }
    $console.filterPassed = if ($t -match 'Passed:\s+(\d+)') { $Matches[1] } else { $null }
    $console.filterSkipped = if ($t -match 'Skipped:\s+(\d+)') { $Matches[1] } else { $null }
    $console.filterTotal = if ($t -match 'Total:\s+(\d+)') { $Matches[1] } else { $null }
    $console.filterDuration = if ($t -match 'Duration:\s+([^\-]+)') { $Matches[1].Trim() } else { $null }
    $console.filterFailedBang = ($t -match 'Failed!')
    $console.filterPassedBang = ($t -match 'Passed!')
    $console.filterRejectMsgCount = ([regex]::Matches($t, 'must reject PLUGIN_ROOT_OVERRIDE')).Count
}
$console | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $out 'dotnet-rerun-console-counts.json') -Encoding utf8
Write-Output 'PARSE_RERUN_DONE'
