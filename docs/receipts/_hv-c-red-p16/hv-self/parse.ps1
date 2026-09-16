#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'

$out = 'F:\GitHub\McpServer\docs\receipts\_hv-c-red-p16\hv-self'

function Get-Attr {
    param($node, [string]$name)
    if ($null -eq $node) { return $null }
    $a = $node.Attributes[$name]
    if ($null -eq $a) { return $null }
    return [string]$a.Value
}

$trxPath = Join-Path $out 'trx-p16-hv\p16-filter.trx'
if (-not (Test-Path -LiteralPath $trxPath)) {
    $alt = Get-ChildItem -LiteralPath (Join-Path $out 'trx-p16-hv') -Filter '*.trx' -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($null -ne $alt) { $trxPath = $alt.FullName }
}

if (-not (Test-Path -LiteralPath $trxPath)) {
    [ordered]@{ exists = $false; path = $trxPath } | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $out 'filter-trx-summary.json') -Encoding utf8
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
    $p16 = @($outcomes | Where-Object { $_.name -match 'AiTheory_Agent_RequiresValidJsonFields' })
    $p17 = @($outcomes | Where-Object { $_.name -match 'AiTheory_RejectsInvalidJson' })
    $nuke = @($outcomes | Where-Object { $_.name -match 'NukeTarget_SkipIsFailure' })
    $hostKinds = @('Codex', 'ClaudeCode', 'ClaudeCowork', 'Copilot', 'Grok', 'Cline', 'ClineV2', 'OpenCode')
    $byHost = foreach ($h in $hostKinds) {
        if ($h -eq 'Cline') {
            $matched = @($p16 | Where-Object { $_.name -match 'hostKind: Cline\)' })
        } elseif ($h -eq 'ClineV2') {
            $matched = @($p16 | Where-Object { $_.name -match 'hostKind: ClineV2\)' })
        } else {
            $matched = @($p16 | Where-Object { $_.name -match ("hostKind: " + $h + '\)') })
        }
        [ordered]@{
            HostKind = $h
            Count = $matched.Count
            Failed = @($matched | Where-Object { $_.outcome -eq 'Failed' }).Count
            Passed = @($matched | Where-Object { $_.outcome -eq 'Passed' }).Count
            Skipped = @($matched | Where-Object { $_.outcome -in @('NotExecuted', 'Skipped', 'Inconclusive') }).Count
            Message = $(if ($matched.Count -gt 0) { $matched[0].message } else { $null })
        }
    }
    [ordered]@{
        exists = $true
        path = $trxPath
        lastWriteTimeUtc = (Get-Item -LiteralPath $trxPath).LastWriteTimeUtc.ToString('o')
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
        nukeCount = $nuke.Count
        notImplementedMessageCount = @($outcomes | Where-Object { $_.message -match 'AiStrategyFixture\.EvaluateAsync is not implemented' }).Count
        p16ByHost = @($byHost)
    } | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $out 'filter-trx-summary.json') -Encoding utf8
}

$filterLog = Join-Path $out 'hv-dotnet-p16-filter.log'
$listLog = Join-Path $out 'hv-dotnet-list-tests.log'
$console = [ordered]@{}
if (Test-Path $filterLog) {
    $t = Get-Content -LiteralPath $filterLog -Raw
    $console.filterFailed = if ($t -match 'Failed:\s+(\d+)') { $Matches[1] } else { $null }
    $console.filterPassed = if ($t -match 'Passed:\s+(\d+)') { $Matches[1] } else { $null }
    $console.filterSkipped = if ($t -match 'Skipped:\s+(\d+)') { $Matches[1] } else { $null }
    $console.filterTotal = if ($t -match 'Total:\s+(\d+)') { $Matches[1] } else { $null }
    $console.filterDuration = if ($t -match 'Duration:\s+([^\r\n]+)') { $Matches[1].Trim() } else { $null }
    $console.totalTestsLine = if ($t -match 'Total tests:\s+(\d+)') { $Matches[1] } else { $null }
    $console.failedLine = if ($t -match 'Failed:\s+(\d+)') { $Matches[1] } else { $null }
    $console.filterFailedBang = ($t -match 'Failed!')
    $console.filterPassedBang = ($t -match 'Passed!')
    $console.testRunFailed = ($t -match 'Test Run Failed')
    $console.notImplementedCount = ([regex]::Matches($t, 'AiStrategyFixture\.EvaluateAsync is not implemented')).Count
    $console.totalTime = if ($t -match 'Total time:\s+([^\r\n]+)') { $Matches[1].Trim() } else { $null }
}
if (Test-Path $listLog) {
    $lt = Get-Content -LiteralPath $listLog
    $console.listFqnApprox = @($lt | Where-Object { $_ -match 'McpServer\.PluginIntegration\.Tests' }).Count
    $console.listP16 = @($lt | Where-Object { $_ -match 'AiTheory_Agent_RequiresValidJsonFields' }).Count
    $console.listP17 = @($lt | Where-Object { $_ -match 'AiTheory_RejectsInvalidJson' }).Count
    $console.listNuke = @($lt | Where-Object { $_ -match 'NukeTarget_SkipIsFailure' }).Count
    $console.listAiTheory = @($lt | Where-Object { $_ -match 'AiTheory_' }).Count
    $console.listSkipAttr = @($lt | Where-Object { $_ -match '(?i)\[skip' }).Count
    $hosts = [ordered]@{}
    foreach ($h in @('Codex', 'ClaudeCode', 'ClaudeCowork', 'Copilot', 'Grok', 'ClineV2', 'OpenCode')) {
        $hosts[$h] = @($lt | Where-Object { $_ -match 'AiTheory_Agent_RequiresValidJsonFields' -and $_ -match ("hostKind: " + $h + '\)') }).Count
    }
    $hosts['Cline'] = @($lt | Where-Object { $_ -match 'AiTheory_Agent_RequiresValidJsonFields' -and $_ -match 'hostKind: Cline\)' }).Count
    $console.listHosts = $hosts
}
$console | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $out 'dotnet-console-counts.json') -Encoding utf8
Write-Output 'PARSE_DONE'
