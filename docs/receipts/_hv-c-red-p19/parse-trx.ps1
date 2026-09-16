#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'

$out = 'F:\GitHub\McpServer\docs\receipts\_hv-c-red-p19'

function Get-Attr {
    param($node, [string]$name)
    if ($null -eq $node) { return $null }
    $a = $node.Attributes[$name]
    if ($null -eq $a) { return $null }
    return [string]$a.Value
}

$trxFiles = @()
if (Test-Path -LiteralPath (Join-Path $out 'trx-files.txt')) {
    $trxFiles = @(Get-Content -LiteralPath (Join-Path $out 'trx-files.txt') | Where-Object { $_ -and (Test-Path -LiteralPath $_) })
}
if ($trxFiles.Count -eq 0) {
    $trxFiles = @(Get-ChildItem -LiteralPath $out -Recurse -Filter '*.trx' -ErrorAction SilentlyContinue | ForEach-Object { $_.FullName })
}

$summaries = foreach ($trx in $trxFiles) {
    [xml]$x = Get-Content -LiteralPath $trx
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
    $p19 = @($outcomes | Where-Object { $_.name -match 'PluginNativeSuite_|PluginInt_P19_' })
    $p20 = @($outcomes | Where-Object { $_.name -match 'PluginSessionLogHarness_AgainstUpdateService|PluginPromotion_StagingOrProduction' })
    $failedLine = @($outcomes | Where-Object { $_.outcome -eq 'Failed' })
    $skippedLine = @($outcomes | Where-Object { $_.outcome -in @('NotExecuted','Skipped','Inconclusive') })
    $passedLine = @($outcomes | Where-Object { $_.outcome -eq 'Passed' })
    [ordered]@{
        exists = $true
        path = $trx
        lastWriteTimeUtc = (Get-Item -LiteralPath $trx).LastWriteTimeUtc.ToString('o')
        length = (Get-Item -LiteralPath $trx).Length
        outcome = Get-Attr $summary 'outcome'
        total = Get-Attr $c 'total'
        executed = Get-Attr $c 'executed'
        passed = Get-Attr $c 'passed'
        failed = Get-Attr $c 'failed'
        skipped = Get-Attr $c 'skipped'
        notExecuted = Get-Attr $c 'notExecuted'
        inconclusive = Get-Attr $c 'inconclusive'
        unitCount = @($results).Count
        p19Count = @($p19).Count
        p19Failed = @($p19 | Where-Object { $_.outcome -eq 'Failed' }).Count
        p19Passed = @($p19 | Where-Object { $_.outcome -eq 'Passed' }).Count
        p19Skipped = @($p19 | Where-Object { $_.outcome -in @('NotExecuted','Skipped','Inconclusive') }).Count
        p20Count = @($p20).Count
        failedNames = @($failedLine | ForEach-Object { $_.name })
        passedNames = @($passedLine | ForEach-Object { $_.name })
        skippedNames = @($skippedLine | ForEach-Object { $_.name })
        failedMessages = @($failedLine | ForEach-Object { [ordered]@{ name = $_.name; message = $_.message } })
        outcomes = $outcomes
        fileNotFoundCount = @($failedLine | Where-Object { $_.message -match 'No docs/receipts/pluginint-p19-<utc> receipt directory exists' }).Count
        fileNotFoundAllFailed = (@($failedLine).Count -gt 0 -and (@($failedLine | Where-Object { $_.message -match 'No docs/receipts/pluginint-p19-<utc> receipt directory exists' }).Count -eq @($failedLine).Count))
    }
}

$summaries | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath (Join-Path $out 'p19-trx-summary.json') -Encoding utf8
Write-Output ('TRX_COUNT=' + @($summaries).Count)
Write-Output 'PARSE_TRX_DONE'
