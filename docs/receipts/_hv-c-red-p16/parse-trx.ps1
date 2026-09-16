#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'

$out = 'F:\GitHub\McpServer\docs\receipts\_hv-c-red-p16'

function Get-Attr {
    param($node, [string]$name)
    if ($null -eq $node) { return $null }
    $a = $node.Attributes[$name]
    if ($null -eq $a) { return $null }
    return [string]$a.Value
}

$trxFiles = @(Get-ChildItem -LiteralPath $out -Filter '*.trx' -Recurse -ErrorAction SilentlyContinue)
$trxFiles | ForEach-Object { $_.FullName } | Set-Content -LiteralPath (Join-Path $out 'trx-files.txt') -Encoding utf8

$chosen = $null
$exitJsonPath = Join-Path $out 'hv-dotnet-p16-filter-exit.json'
if (Test-Path -LiteralPath $exitJsonPath) {
    $exitJson = Get-Content -LiteralPath $exitJsonPath -Raw | ConvertFrom-Json
    if ($exitJson.TrxFiles) {
        $chosen = @($exitJson.TrxFiles) | Select-Object -First 1
    }
}
if (-not $chosen -and $trxFiles.Count -gt 0) {
    $chosen = ($trxFiles | Sort-Object LastWriteTimeUtc -Descending | Select-Object -First 1).FullName
}

if (-not $chosen -or -not (Test-Path -LiteralPath $chosen)) {
    [ordered]@{ exists = $false; path = [string]$chosen } | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $out 'filter-trx-summary.json') -Encoding utf8
    Write-Output 'NO_TRX'
    exit 1
}

[xml]$x = Get-Content -LiteralPath $chosen
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

$hostKinds = @('Codex', 'ClaudeCode', 'ClaudeCowork', 'Copilot', 'Grok', 'Cline', 'ClineV2', 'OpenCode')
$p16 = @($outcomes | Where-Object { $_.name -match 'AiTheory_Agent_RequiresValidJsonFields' })
$p17 = @($outcomes | Where-Object { $_.name -match 'AiTheory_RejectsInvalidJson' })
$p18 = @($outcomes | Where-Object { $_.name -match 'NukeTarget_SkipIsFailure' })
$byHost = foreach ($h in $hostKinds) {
    $matched = if ($h -eq 'Cline') {
        @($p16 | Where-Object { $_.name -match 'Cline\)' -and $_.name -notmatch 'ClineV2' })
    } elseif ($h -eq 'ClineV2') {
        @($p16 | Where-Object { $_.name -match 'ClineV2' })
    } else {
        @($p16 | Where-Object { $_.name -match ([regex]::Escape($h) + '\)') })
    }
    [ordered]@{
        HostKind = $h
        Count = $matched.Count
        Failed = @($matched | Where-Object { $_.outcome -eq 'Failed' }).Count
        Passed = @($matched | Where-Object { $_.outcome -eq 'Passed' }).Count
        Skipped = @($matched | Where-Object { $_.outcome -in @('NotExecuted', 'Skipped', 'Inconclusive') }).Count
        Messages = @($matched | ForEach-Object { $_.message })
    }
}

$logPath = Join-Path $out 'hv-dotnet-p16-filter.log'
$logText = if (Test-Path -LiteralPath $logPath) { Get-Content -LiteralPath $logPath -Raw } else { '' }
$listLog = Join-Path $out 'hv-dotnet-list-tests.log'
$listText = if (Test-Path -LiteralPath $listLog) { Get-Content -LiteralPath $listLog -Raw } else { '' }

[ordered]@{
    exists = $true
    path = $chosen
    lastWriteTimeUtc = (Get-Item -LiteralPath $chosen).LastWriteTimeUtc.ToString('o')
    length = (Get-Item -LiteralPath $chosen).Length
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
    failedMessages = @($outcomes | Where-Object { $_.outcome -eq 'Failed' } | ForEach-Object { [ordered]@{ name = $_.name; message = $_.message } })
    p16Count = $p16.Count
    p16Failed = @($p16 | Where-Object { $_.outcome -eq 'Failed' }).Count
    p16Passed = @($p16 | Where-Object { $_.outcome -eq 'Passed' }).Count
    p16Skipped = @($p16 | Where-Object { $_.outcome -in @('NotExecuted','Skipped','Inconclusive') }).Count
    p17Count = $p17.Count
    p18Count = $p18.Count
    unimplementedCount = @($outcomes | Where-Object { $_.message -match 'EvaluateAsync is not implemented' }).Count
    byHost = $byHost
    consoleHasPassedLine = [bool]($logText -match '(?m)^\s*Passed:')
    consoleFailedMatch = if ($logText -match '(?m)Failed:\s*(\d+)') { $Matches[1] } else { $null }
    consoleTotalMatch = if ($logText -match '(?m)Total tests:\s*(\d+)') { $Matches[1] } else { $null }
    consoleSkippedMatch = if ($logText -match '(?m)Skipped:\s*(\d+)') { $Matches[1] } else { $null }
    consolePassedMatch = if ($logText -match '(?m)Passed:\s*(\d+)') { $Matches[1] } else { $null }
    listAiTheory = ([regex]::Matches($listText, 'AiTheory_Agent_RequiresValidJsonFields\(')).Count
    listP17 = ([regex]::Matches($listText, 'AiTheory_RejectsInvalidJson')).Count
    listP18 = ([regex]::Matches($listText, 'NukeTarget_SkipIsFailure')).Count
    listTotalAvailable = ([regex]::Matches($listText, '(?m)^\s+McpServer\.PluginIntegration\.Tests\.')).Count
} | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $out 'filter-trx-summary.json') -Encoding utf8

Write-Output ("TRX=$chosen")
Write-Output 'PARSE_DONE'
