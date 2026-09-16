#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'

$out = 'F:\GitHub\McpServer\docs\receipts\_hv-c-red-p14'

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
    $p11 = @($outcomes | Where-Object { $_.name -match 'Theory_EachScenario_FailsUntilAdapterOperational' })
    $p12 = @($outcomes | Where-Object { $_.name -match 'BootstrapBeginAppendComplete_CapturesIdsCacheShaReceipt' })
    $p13 = @($outcomes | Where-Object { $_.name -match 'ServerQuery_SourceTypeIdsActionDialogCompletedWorkspace' })
    $p14 = @($outcomes | Where-Object { $_.name -match 'PluginRootOverride_WritesOnlyWorkspaceMcpServerAgent_PoisonEmpty' })
    $p15 = @($outcomes | Where-Object { $_.name -match 'Success_NoPendingFailsafe|FailedSubmit_RetainsRootIdPending|RetrySuccess_DeletesOnlyMatchingPending' })
    $p16 = @($outcomes | Where-Object { $_.name -match 'AiTheory_' })
    $rejectMsg = @($outcomes | Where-Object { $_.message -match 'must reject PLUGIN_ROOT_OVERRIDE' })
    $hostKinds = @('Codex', 'ClaudeCode', 'ClaudeCowork', 'Copilot', 'Grok', 'Cline', 'ClineV2', 'OpenCode')
    function Summarize-ByHost($rows) {
        foreach ($h in $hostKinds) {
            $matched = @($rows | Where-Object { $_.name -match [regex]::Escape($h) + '(?!V2)' })
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
        p11Count = $p11.Count
        p11Failed = @($p11 | Where-Object { $_.outcome -eq 'Failed' }).Count
        p11Passed = @($p11 | Where-Object { $_.outcome -eq 'Passed' }).Count
        p12Count = $p12.Count
        p13Count = $p13.Count
        p14Count = $p14.Count
        p14Failed = @($p14 | Where-Object { $_.outcome -eq 'Failed' }).Count
        p14Passed = @($p14 | Where-Object { $_.outcome -eq 'Passed' }).Count
        p14Skipped = @($p14 | Where-Object { $_.outcome -in @('NotExecuted','Skipped','Inconclusive') }).Count
        p15Count = $p15.Count
        p16Count = $p16.Count
        rejectMessageCount = $rejectMsg.Count
        p14ByHost = @(Summarize-ByHost $p14)
        unitOutcomes = $outcomes
    } | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $dest -Encoding utf8
}

Get-TrxSummary -TrxPath (Join-Path $out 'trx-p14\p14-filter.trx') -Name 'filter'

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
        p11 = @($taskObjs | Where-Object { $_.task -match '^P11' })
        p12 = @($taskObjs | Where-Object { $_.task -match '^P12' })
        p13 = @($taskObjs | Where-Object { $_.task -match '^P13' })
        p14 = @($taskObjs | Where-Object { $_.task -match '^P14' })
        p15 = @($taskObjs | Where-Object { $_.task -match '^P15' })
        p16 = @($taskObjs | Where-Object { $_.task -match '^P16' })
        combinedC = @($taskObjs | Where-Object { $_.task -match 'P11 red|P14' })
        tasks = $taskObjs
    } | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $Dest -Encoding utf8
}

Extract-TodoDone -Path (Join-Path $out 'todo-plan.txt') -Dest (Join-Path $out 'todo-plan-done-extract.json')
Extract-TodoDone -Path (Join-Path $out 'todo-pluginint.txt') -Dest (Join-Path $out 'todo-pluginint-done-extract.json')
Extract-TodoDone -Path (Join-Path $out 'todo-plan-client.txt') -Dest (Join-Path $out 'todo-plan-client-done-extract.json')
Extract-TodoDone -Path (Join-Path $out 'todo-pluginint-client.txt') -Dest (Join-Path $out 'todo-pluginint-client-done-extract.json')

$filterLog = Join-Path $out 'dotnet-p14-filter.log'
$listLog = Join-Path $out 'dotnet-list-tests.log'
$console = [ordered]@{}
if (Test-Path $filterLog) {
    $t = Get-Content -LiteralPath $filterLog -Raw
    $console.filterFailed = if ($t -match 'Failed:\s+(\d+)') { $Matches[1] } else { $null }
    $console.filterPassed = if ($t -match 'Passed:\s+(\d+)') { $Matches[1] } else { $null }
    $console.filterSkipped = if ($t -match 'Skipped:\s+(\d+)') { $Matches[1] } else { $null }
    $console.filterTotal = if ($t -match 'Total:\s+(\d+)') { $Matches[1] } else { $null }
    $console.filterFailedBang = ($t -match 'Failed!')
    $console.filterPassedBang = ($t -match 'Passed!')
    $console.filterRejectMsgCount = ([regex]::Matches($t, 'must reject PLUGIN_ROOT_OVERRIDE')).Count
}
if (Test-Path $listLog) {
    $lt = Get-Content -LiteralPath $listLog
    $names = @($lt | Where-Object { $_ -match 'PluginSessionLogWorkflowAdapterTests' -or $_ -match 'Theory_Agent_' -or $_ -match 'AiTheory_' })
    $console.listP14 = @($lt | Where-Object { $_ -match 'PluginRootOverride_WritesOnlyWorkspaceMcpServerAgent_PoisonEmpty' }).Count
    $console.listP15Success = @($lt | Where-Object { $_ -match 'Success_NoPendingFailsafe' }).Count
    $console.listP15FailedSubmit = @($lt | Where-Object { $_ -match 'FailedSubmit_RetainsRootIdPending' }).Count
    $console.listP15Retry = @($lt | Where-Object { $_ -match 'RetrySuccess_DeletesOnlyMatchingPending' }).Count
    $console.listAiTheory = @($lt | Where-Object { $_ -match 'AiTheory_' }).Count
    $console.listAdapterTestLines = @($names | ForEach-Object { $_.Trim() })
}
$console | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $out 'dotnet-console-counts.json') -Encoding utf8

Write-Output 'PARSE_DONE'
