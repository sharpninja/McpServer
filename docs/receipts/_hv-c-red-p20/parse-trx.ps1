#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'

$out = 'F:\GitHub\McpServer\docs\receipts\_hv-c-red-p20'
$trx = Join-Path $out 'results-p20-red\p20-red.trx'
$consoleLog = Join-Path $out 'hv-dotnet-p20-filter.log'

$console = Get-Content -LiteralPath $consoleLog -Raw
$failed = $null; $passed = $null; $skipped = $null; $total = $null; $duration = $null
if ($console -match 'Failed!\s+-\s+Failed:\s+(\d+),\s+Passed:\s+(\d+),\s+Skipped:\s+(\d+),\s+Total:\s+(\d+),\s+Duration:\s+(\d+)\s*ms') {
    $failed = [int]$Matches[1]
    $passed = [int]$Matches[2]
    $skipped = [int]$Matches[3]
    $total = [int]$Matches[4]
    $duration = [int]$Matches[5]
}

$harnessMsg = $null
$promoMsg = $null
if ($console -match '(?s)PluginSessionLogHarness_AgainstUpdateService_SanitizedFixtures_FailedZeroSkippedZero.*?Error Message:\s*(.+?)\r?\n\s*Stack Trace:') {
    $harnessMsg = $Matches[1].Trim()
}
if ($console -match '(?s)PluginPromotion_StagingOrProduction_RequiresOperatorApprovalFlag.*?Error Message:\s*(.+?)\r?\n\s*Stack Trace:') {
    $promoMsg = $Matches[1].Trim()
}

[xml]$trxXml = Get-Content -LiteralPath $trx -Raw
$ns = New-Object System.Xml.XmlNamespaceManager($trxXml.NameTable)
$ns.AddNamespace('t', 'http://microsoft.com/schemas/VisualStudio/TeamTest/2010')
$counters = $trxXml.SelectSingleNode('//t:ResultSummary/t:Counters', $ns)
$skippedAttr = $null
if ($null -ne $counters -and $null -ne $counters.Attributes -and $null -ne $counters.Attributes['skipped']) {
    $skippedAttr = $counters.Attributes['skipped'].Value
}
$outcomes = @($trxXml.SelectNodes('//t:UnitTestResult', $ns) | ForEach-Object {
    $messageNode = $_.SelectSingleNode('t:Output/t:ErrorInfo/t:Message', $ns)
    [ordered]@{
        name = [string]$_.testName
        outcome = [string]$_.outcome
        duration = [string]$_.duration
        message = if ($messageNode) { $messageNode.InnerText.Trim() } else { $null }
    }
})

[ordered]@{
    consoleFailed = $failed
    consolePassed = $passed
    consoleSkipped = $skipped
    consoleTotal = $total
    consoleDurationMs = $duration
    consoleHarnessMessage = $harnessMsg
    consolePromotionMessage = $promoMsg
    trxTotal = [string]$counters.total
    trxExecuted = [string]$counters.executed
    trxPassed = [string]$counters.passed
    trxFailed = [string]$counters.failed
    trxNotExecuted = [string]$counters.notExecuted
    trxSkippedAttr = $skippedAttr
    outcomes = $outcomes
} | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $out 'p20-trx-summary.json') -Encoding utf8

Write-Output ("CONSOLE Failed=$failed Passed=$passed Skipped=$skipped Total=$total DurationMs=$duration")
Write-Output ("TRX total=$($counters.total) executed=$($counters.executed) passed=$($counters.passed) failed=$($counters.failed) notExecuted=$($counters.notExecuted) skippedAttr=$skippedAttr")
Write-Output ("HARNESS_MSG=$harnessMsg")
Write-Output ("PROMO_MSG=$promoMsg")
