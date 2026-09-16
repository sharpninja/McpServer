#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'

$out = 'F:\GitHub\McpServer\docs\receipts\_hv-c-green-p15'

function Get-Attr {
    param($node, [string]$name)
    if ($null -eq $node) { return $null }
    $a = $node.Attributes[$name]
    if ($null -eq $a) { return $null }
    return [string]$a.Value
}

function Get-TrxSummary {
    param([string]$TrxPath, [string]$Name)
    $dest = Join-Path $out ($Name + '-trx-summary.json')
    if (-not (Test-Path -LiteralPath $TrxPath)) {
        [ordered]@{ exists = $false; path = $TrxPath } | ConvertTo-Json | Set-Content -LiteralPath $dest -Encoding utf8
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
        $stdNode = $u.SelectSingleNode('t:Output/t:StdOut', $ns)
        $outcomes += [ordered]@{
            name = Get-Attr $u 'testName'
            outcome = Get-Attr $u 'outcome'
            duration = Get-Attr $u 'duration'
            message = $(if ($msgNode) { [string]$msgNode.InnerText } else { $null })
            stdout = $(if ($stdNode) { [string]$stdNode.InnerText } else { $null })
        }
    }
    $p15 = @($outcomes | Where-Object { $_.name -match 'Success_NoPendingFailsafe|FailedSubmit_RetainsRootIdPending|RetrySuccess_DeletesOnlyMatchingPending' })
    $p16 = @($outcomes | Where-Object { $_.name -match 'AiTheory_' })
    $success = @($outcomes | Where-Object { $_.name -match 'Success_NoPendingFailsafe' })
    $failedSubmit = @($outcomes | Where-Object { $_.name -match 'FailedSubmit_RetainsRootIdPending' })
    $retry = @($outcomes | Where-Object { $_.name -match 'RetrySuccess_DeletesOnlyMatchingPending' })
    $hostKinds = @('Codex', 'ClaudeCode', 'ClaudeCowork', 'Copilot', 'Grok', 'Cline', 'ClineV2', 'OpenCode')
    $p15ByHost = foreach ($h in $hostKinds) {
        $matched = if ($h -eq 'Cline') {
            @($p15 | Where-Object { $_.name -match 'Cline\)' -and $_.name -notmatch 'ClineV2' })
        } elseif ($h -eq 'ClineV2') {
            @($p15 | Where-Object { $_.name -match 'ClineV2' })
        } else {
            @($p15 | Where-Object { $_.name -match ([regex]::Escape($h) + '\)') })
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
    [ordered]@{
        exists = $true
        path = $TrxPath
        lastWriteTimeUtc = (Get-Item -LiteralPath $TrxPath).LastWriteTimeUtc.ToString('o')
        length = (Get-Item -LiteralPath $TrxPath).Length
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
        p15Count = $p15.Count
        p15Failed = @($p15 | Where-Object { $_.outcome -eq 'Failed' }).Count
        p15Passed = @($p15 | Where-Object { $_.outcome -eq 'Passed' }).Count
        p15Skipped = @($p15 | Where-Object { $_.outcome -in @('NotExecuted','Skipped','Inconclusive') }).Count
        p15SuccessCount = $success.Count
        p15SuccessPassed = @($success | Where-Object { $_.outcome -eq 'Passed' }).Count
        p15FailedSubmitCount = $failedSubmit.Count
        p15FailedSubmitPassed = @($failedSubmit | Where-Object { $_.outcome -eq 'Passed' }).Count
        p15RetryCount = $retry.Count
        p15RetryPassed = @($retry | Where-Object { $_.outcome -eq 'Passed' }).Count
        p16Count = $p16.Count
        p15ByHost = @($p15ByHost)
        unitOutcomes = $outcomes
    } | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $dest -Encoding utf8
}

function Extract-TodoDone {
    param([string]$Path, [string]$Dest)
    if (-not (Test-Path -LiteralPath $Path)) {
        [ordered]@{ exists = $false } | ConvertTo-Json | Set-Content -LiteralPath $Dest -Encoding utf8
        return
    }
    $text = Get-Content -LiteralPath $Path -Raw
    $topDone = $false
    if ($text -match '(?m)^\s+done:\s+(true|false)\s*$') { $topDone = ($Matches[1] -eq 'true') }
    $tasks = [regex]::Matches($text, '(?m)^\s+- task:\s*(.+?)\r?\n\s+done:\s+(true|false)')
    $taskObjs = foreach ($m in $tasks) {
        [pscustomobject]@{ task = $m.Groups[1].Value.Trim(); done = ($m.Groups[2].Value -eq 'true') }
    }
    [ordered]@{
        exists = $true
        topDone = $topDone
        taskCount = @($taskObjs).Count
        doneTrueCount = @($taskObjs | Where-Object { $_.done }).Count
        p14 = @($taskObjs | Where-Object { $_.task -match 'P14' })
        p15 = @($taskObjs | Where-Object { $_.task -match 'P15' })
        combinedC = @($taskObjs | Where-Object { $_.task -match 'C P14' })
        tasks = $taskObjs
    } | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $Dest -Encoding utf8
}

function Get-ConsoleCounts {
    param([string]$LogPath, [string]$Dest)
    $console = [ordered]@{ exists = (Test-Path -LiteralPath $LogPath); path = $LogPath }
    if (Test-Path -LiteralPath $LogPath) {
        $t = Get-Content -LiteralPath $LogPath -Raw
        $console.failed = if ($t -match '(?m)^Failed:\s+(\d+)') { [int]$Matches[1] } else { if ($t -match 'Failed:\s+(\d+)') { [int]$Matches[1] } else { $null } }
        $console.passed = if ($t -match '(?m)^Passed:\s+(\d+)') { [int]$Matches[1] } else { if ($t -match 'Passed:\s+(\d+)') { [int]$Matches[1] } else { $null } }
        $console.skipped = if ($t -match '(?m)^Skipped:\s+(\d+)') { [int]$Matches[1] } else { if ($t -match 'Skipped:\s+(\d+)') { [int]$Matches[1] } else { $null } }
        $console.total = if ($t -match '(?m)^Total:\s+(\d+)') { [int]$Matches[1] } else { if ($t -match 'Total:\s+(\d+)') { [int]$Matches[1] } else { $null } }
        $console.failedBang = ($t -match 'Failed!')
        $console.passedBang = ($t -match 'Passed!')
        $console.duration = if ($t -match 'Duration:\s+([^\r\n]+)') { $Matches[1].Trim() } else { $null }
        $item = Get-Item -LiteralPath $LogPath
        $console.length = $item.Length
        $console.lastWriteTimeUtc = $item.LastWriteTimeUtc.ToString('o')
        $summaryLine = [regex]::Match($t, '(?m)^(?:Passed|Failed)!  - Failed:\s+\d+.*$')
        $console.summaryLine = if ($summaryLine.Success) { $summaryLine.Value.Trim() } else { $null }
        $allSummaries = @([regex]::Matches($t, '(?m)^(?:Passed|Failed)!  - Failed:\s+\d+, Passed:\s+\d+, Skipped:\s+\d+, Total:\s+\d+, Duration:\s+.+$') | ForEach-Object { $_.Value.Trim() })
        $console.allSummaryLines = $allSummaries
    }
    $console | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $Dest -Encoding utf8
}

Get-TrxSummary -TrxPath (Join-Path $out 'trx-p15-hv\p15-filter.trx') -Name 'filter'
Get-TrxSummary -TrxPath (Join-Path $out 'trx-all-hv\pluginint-all.trx') -Name 'full'

Extract-TodoDone -Path (Join-Path $out 'todo-plan.txt') -Dest (Join-Path $out 'todo-plan-done-extract.json')
Extract-TodoDone -Path (Join-Path $out 'todo-pluginint.txt') -Dest (Join-Path $out 'todo-pluginint-done-extract.json')
Extract-TodoDone -Path (Join-Path $out 'todo-plan-client.txt') -Dest (Join-Path $out 'todo-plan-client-done-extract.json')
Extract-TodoDone -Path (Join-Path $out 'todo-pluginint-client.txt') -Dest (Join-Path $out 'todo-pluginint-client-done-extract.json')

Get-ConsoleCounts -LogPath (Join-Path $out 'hv-dotnet-p15-filter.log') -Dest (Join-Path $out 'hv-dotnet-p15-filter-console.json')
Get-ConsoleCounts -LogPath (Join-Path $out 'hv-dotnet-pluginint-all.log') -Dest (Join-Path $out 'hv-dotnet-pluginint-all-console.json')

$listLog = Join-Path $out 'hv-dotnet-list-tests.log'
if (Test-Path -LiteralPath $listLog) {
    $lt = Get-Content -LiteralPath $listLog
    [ordered]@{
        totalListed = @($lt | Where-Object { $_ -match '^\s{2,}' -and $_ -match '\.' }).Count
        p15Success = @($lt | Where-Object { $_ -match 'Success_NoPendingFailsafe' }).Count
        p15FailedSubmit = @($lt | Where-Object { $_ -match 'FailedSubmit_RetainsRootIdPending' }).Count
        p15Retry = @($lt | Where-Object { $_ -match 'RetrySuccess_DeletesOnlyMatchingPending' }).Count
        p16AiTheory = @($lt | Where-Object { $_ -match 'AiTheory_' }).Count
        skipListed = @($lt | Where-Object { $_ -match '\[SKIP\]|Skipped' }).Count
        p15Lines = @($lt | Where-Object { $_ -match 'Success_NoPendingFailsafe|FailedSubmit_RetainsRootIdPending|RetrySuccess_DeletesOnlyMatchingPending' } | ForEach-Object { $_.Trim() })
    } | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $out 'list-tests-summary.json') -Encoding utf8
}

Write-Output 'PARSE_DONE'
