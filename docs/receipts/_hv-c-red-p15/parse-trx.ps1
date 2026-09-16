#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'

$out = 'F:\GitHub\McpServer\docs\receipts\_hv-c-red-p15'

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
        $stackNode = $u.SelectSingleNode('t:Output/t:ErrorInfo/t:StackTrace', $ns)
        $outcomes += [ordered]@{
            name = Get-Attr $u 'testName'
            outcome = Get-Attr $u 'outcome'
            duration = Get-Attr $u 'duration'
            message = $(if ($msgNode) { [string]$msgNode.InnerText } else { $null })
            stack = $(if ($stackNode) { [string]$stackNode.InnerText } else { $null })
        }
    }
    $p14 = @($outcomes | Where-Object { $_.name -match 'PluginRootOverride_WritesOnlyWorkspaceMcpServerAgent_PoisonEmpty' })
    $p15Success = @($outcomes | Where-Object { $_.name -match 'Success_NoPendingFailsafe' })
    $p15Failed = @($outcomes | Where-Object { $_.name -match 'FailedSubmit_RetainsRootIdPending' })
    $p15Retry = @($outcomes | Where-Object { $_.name -match 'RetrySuccess_DeletesOnlyMatchingPending' })
    $p15 = @($p15Success + $p15Failed + $p15Retry)
    $p16 = @($outcomes | Where-Object { $_.name -match 'AiTheory_' })
    $failsafeMsg = @($outcomes | Where-Object { $_.message -match 'did not verify the V4 failsafe pending path' })
    $notImplFailed = @($outcomes | Where-Object { $_.message -match 'ExecuteFailedSubmitAsync is not implemented' })
    $notImplRetry = @($outcomes | Where-Object { $_.message -match 'RetryFailedSubmitAsync is not implemented' })
    $hostKinds = @('Codex', 'ClaudeCode', 'ClaudeCowork', 'Copilot', 'Grok', 'Cline', 'ClineV2', 'OpenCode')
    function Summarize-ByHost($rows) {
        foreach ($h in $hostKinds) {
            $matched = @($rows | Where-Object { $_.name -match [regex]::Escape($h) })
            if ($h -eq 'Cline') {
                $matched = @($rows | Where-Object { $_.name -match 'Cline\)' -and $_.name -notmatch 'ClineV2' })
            } elseif ($h -eq 'ClineV2') {
                $matched = @($rows | Where-Object { $_.name -match 'ClineV2' })
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
    }
    [ordered]@{
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
        failedNames = @($outcomes | Where-Object { $_.outcome -eq 'Failed' } | ForEach-Object { $_.name })
        passedNames = @($outcomes | Where-Object { $_.outcome -eq 'Passed' } | ForEach-Object { $_.name })
        skippedNames = @($outcomes | Where-Object { $_.outcome -in @('NotExecuted','Skipped','Inconclusive') } | ForEach-Object { $_.name })
        failedMessages = @($outcomes | Where-Object { $_.outcome -eq 'Failed' } | ForEach-Object { [ordered]@{ name = $_.name; message = $_.message } })
        p14Count = $p14.Count
        p15Count = $p15.Count
        p15SuccessCount = $p15Success.Count
        p15SuccessFailed = @($p15Success | Where-Object { $_.outcome -eq 'Failed' }).Count
        p15SuccessPassed = @($p15Success | Where-Object { $_.outcome -eq 'Passed' }).Count
        p15FailedSubmitCount = $p15Failed.Count
        p15FailedSubmitFailed = @($p15Failed | Where-Object { $_.outcome -eq 'Failed' }).Count
        p15FailedSubmitPassed = @($p15Failed | Where-Object { $_.outcome -eq 'Passed' }).Count
        p15RetryCount = $p15Retry.Count
        p15RetryFailed = @($p15Retry | Where-Object { $_.outcome -eq 'Failed' }).Count
        p15RetryPassed = @($p15Retry | Where-Object { $_.outcome -eq 'Passed' }).Count
        p15Passed = @($p15 | Where-Object { $_.outcome -eq 'Passed' }).Count
        p15Failed = @($p15 | Where-Object { $_.outcome -eq 'Failed' }).Count
        p15Skipped = @($p15 | Where-Object { $_.outcome -in @('NotExecuted','Skipped','Inconclusive') }).Count
        p16Count = $p16.Count
        failsafeVerifiedFalseMessageCount = $failsafeMsg.Count
        executeFailedNotImplementedCount = $notImplFailed.Count
        retryNotImplementedCount = $notImplRetry.Count
        p15SuccessByHost = @(Summarize-ByHost $p15Success)
        p15FailedSubmitByHost = @(Summarize-ByHost $p15Failed)
        p15RetryByHost = @(Summarize-ByHost $p15Retry)
        unitOutcomes = $outcomes
    } | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $dest -Encoding utf8
}

Get-TrxSummary -TrxPath (Join-Path $out 'trx-p15-hv\p15-filter.trx') -Name 'filter'

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
        p14 = @($taskObjs | Where-Object { $_.task -match '^P14' })
        p15 = @($taskObjs | Where-Object { $_.task -match '^P15' })
        p16 = @($taskObjs | Where-Object { $_.task -match '^P16' })
        combinedC = @($taskObjs | Where-Object { $_.task -match 'P14 red|P15 red' })
        tasks = $taskObjs
    } | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $Dest -Encoding utf8
}

Extract-TodoDone -Path (Join-Path $out 'todo-plan.txt') -Dest (Join-Path $out 'todo-plan-done-extract.json')
Extract-TodoDone -Path (Join-Path $out 'todo-pluginint.txt') -Dest (Join-Path $out 'todo-pluginint-done-extract.json')
Extract-TodoDone -Path (Join-Path $out 'todo-plan-client.txt') -Dest (Join-Path $out 'todo-plan-client-done-extract.json')
Extract-TodoDone -Path (Join-Path $out 'todo-pluginint-client.txt') -Dest (Join-Path $out 'todo-pluginint-client-done-extract.json')

$filterLog = Join-Path $out 'hv-dotnet-p15-filter.log'
$listLog = Join-Path $out 'hv-dotnet-list-tests.log'
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
    $console.failsafeVerifiedMsgCount = ([regex]::Matches($t, 'did not verify the V4 failsafe pending path')).Count
    $console.executeFailedNotImplCount = ([regex]::Matches($t, 'ExecuteFailedSubmitAsync is not implemented')).Count
    $console.retryNotImplCount = ([regex]::Matches($t, 'RetryFailedSubmitAsync is not implemented')).Count
}
if (Test-Path $listLog) {
    $lt = Get-Content -LiteralPath $listLog
    $fqn = @($lt | Where-Object { $_ -match 'McpServer\.PluginIntegration\.Tests' -or $_ -match 'Theory_' -or $_ -match 'AiTheory_' })
    $console.listFqnApprox = $fqn.Count
    $console.listP14 = @($lt | Where-Object { $_ -match 'PluginRootOverride_WritesOnlyWorkspaceMcpServerAgent_PoisonEmpty' }).Count
    $console.listP15Success = @($lt | Where-Object { $_ -match 'Success_NoPendingFailsafe' }).Count
    $console.listP15FailedSubmit = @($lt | Where-Object { $_ -match 'FailedSubmit_RetainsRootIdPending' }).Count
    $console.listP15Retry = @($lt | Where-Object { $_ -match 'RetrySuccess_DeletesOnlyMatchingPending' }).Count
    $console.listAiTheory = @($lt | Where-Object { $_ -match 'AiTheory_' }).Count
    $console.listSkip = @($lt | Where-Object { $_ -match '(?i)skip' }).Count
}
$console | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $out 'dotnet-console-counts.json') -Encoding utf8

Write-Output 'PARSE_DONE'
