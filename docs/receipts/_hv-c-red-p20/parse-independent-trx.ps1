#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$out = 'F:\GitHub\McpServer\docs\receipts\_hv-c-red-p20'
$trx = 'F:\GitHub\McpServer\docs\receipts\_hv-c-red-p20\results-p20-red-independent\p20-red-independent.trx'
$consoleLog = Join-Path $out 'independent-dotnet-p20-filter.log'
$console = Get-Content -LiteralPath $consoleLog -Raw
$plain = [regex]::Replace($console, '\x1B\[[0-9;]*[A-Za-z]', '')

$failed = $null; $passed = $null; $skipped = $null; $total = $null
if ($plain -match '(?m)Failed!\s+Failed:\s+(\d+),\s+Passed:\s+(\d+),\s+Skipped:\s+(\d+),\s+Total:\s+(\d+)') {
    $failed = [int]$Matches[1]; $passed = [int]$Matches[2]; $skipped = [int]$Matches[3]; $total = [int]$Matches[4]
}

[xml]$trxXml = Get-Content -LiteralPath $trx -Raw
$ns = New-Object System.Xml.XmlNamespaceManager($trxXml.NameTable)
$ns.AddNamespace('t', 'http://microsoft.com/schemas/VisualStudio/TeamTest/2010')
$counters = $trxXml.SelectSingleNode('//t:ResultSummary/t:Counters', $ns)
$results = @($trxXml.SelectNodes('//t:UnitTestResult', $ns) | ForEach-Object {
    $messageNode = $_.SelectSingleNode('t:Output/t:ErrorInfo/t:Message', $ns)
    [ordered]@{
        name = $_.testName
        outcome = $_.outcome
        duration = $_.duration
        message = if ($messageNode) { $messageNode.InnerText } else { $null }
    }
})

$skippedAttr = $null
if ($null -ne $counters -and $null -ne $counters.Attributes['skipped']) {
    $skippedAttr = $counters.skipped
}

[ordered]@{
    trxExists = $true
    total = $counters.total
    executed = $counters.executed
    passed = $counters.passed
    failed = $counters.failed
    notExecuted = $counters.notExecuted
    skippedAttr = $skippedAttr
    outcomes = $results
} | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $out 'independent-p20-trx-summary.json') -Encoding utf8

[ordered]@{
    command = 'dotnet test tests/McpServer.PluginIntegration.Tests -c Debug --filter FullyQualifiedName~PluginUpdateServiceHarnessTests --logger trx;LogFileName=p20-red-independent.trx --results-directory F:\GitHub\McpServer\docs\receipts\_hv-c-red-p20\results-p20-red-independent -m:1'
    consoleFailed = $failed
    consolePassed = $passed
    consoleSkipped = $skipped
    consoleTotal = $total
    trxPath = $trx
    trxExists = $true
    usedNoBuild = $false
    harnessFileNotFound = [bool]($plain -match 'FileNotFoundException\s*:\s*No docs/receipts/pluginint-p20-<utc> receipt directory exists\.')
    promotionFileNotFound = [bool]($plain -match 'FileNotFoundException\s*:\s*Build\.PluginPromotion\.cs is missing\.')
    rebuilt = [bool]($plain -match 'McpServer\.PluginIntegration\.Tests ->')
} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $out 'independent-dotnet-p20-filter-exit.json') -Encoding utf8

function Extract-Done {
    param([string]$Path, [string]$Id)
    $text = Get-Content -LiteralPath $Path -Raw
    $top = $null
    if ($text -match '(?m)^\s+done:\s*(true|false)\s*$') { $top = $Matches[1] }
    $p20 = @()
    $lines = Get-Content -LiteralPath $Path
    for ($i = 0; $i -lt $lines.Count; $i++) {
        if ($lines[$i] -match 'P20') {
            $done = $null
            if (($i + 1) -lt $lines.Count -and $lines[$i + 1] -match 'done:\s*(true|false)') { $done = $Matches[1] }
            $p20 += [ordered]@{ line = ($i + 1); text = $lines[$i].Trim(); nextDone = $done }
        }
    }
    [ordered]@{ id = $Id; topDone = $top; p20Hits = $p20 }
}

$plan = Extract-Done -Path (Join-Path $out 'independent-todo-plan-client.txt') -Id 'PLAN-PLUGINHANDOFF-001'
$pluginint = Extract-Done -Path (Join-Path $out 'independent-todo-pluginint-client.txt') -Id 'MCP-PLUGININT-001'
$hygiene = Extract-Done -Path (Join-Path $out 'independent-todo-hygiene-client.txt') -Id 'MCP-WORKSPACEHYGIENE-002'
[ordered]@{ plan = $plan; pluginint = $pluginint; hygiene = $hygiene } | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $out 'independent-todo-done-extract.json') -Encoding utf8

Write-Output ('TRX_PARSE_OK FAILED=' + $failed + ' PASSED=' + $passed + ' SKIPPED=' + $skipped)
