#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'

$out = 'F:\GitHub\McpServer\docs\receipts\_hv-c-green-p7-p10'

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
    $p7 = @($outcomes | Where-Object { $_.name -match 'Adapter_CapturesExecutableArgsStdinEnvCwdTimeoutExitStdoutStderr' })
    $p8 = @($outcomes | Where-Object { $_.name -match 'Adapter_CodexAndPowerShellHook_UseDeclaredEntrypoint' })
    $p9 = @($outcomes | Where-Object { $_.name -match 'Adapter_ClineV1_StdioSessionLogTools' })
    $p10 = @($outcomes | Where-Object { $_.name -match 'Adapter_ClineV2AndOpenCode_UseProductionExport' })
    $p11 = @($outcomes | Where-Object { $_.name -match 'Theory_EachScenario_FailsUntilAdapterOperational' })
    $adapterClass = @($outcomes | Where-Object { $_.name -match 'PluginHostProcessAdapterTests' })
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
        p7Count = $p7.Count
        p7Failed = @($p7 | Where-Object { $_.outcome -eq 'Failed' }).Count
        p7Passed = @($p7 | Where-Object { $_.outcome -eq 'Passed' }).Count
        p8Count = $p8.Count
        p8Passed = @($p8 | Where-Object { $_.outcome -eq 'Passed' }).Count
        p9Count = $p9.Count
        p9Passed = @($p9 | Where-Object { $_.outcome -eq 'Passed' }).Count
        p10Count = $p10.Count
        p10Passed = @($p10 | Where-Object { $_.outcome -eq 'Passed' }).Count
        p11Count = $p11.Count
        adapterClassCount = $adapterClass.Count
        adapterClassPassed = @($adapterClass | Where-Object { $_.outcome -eq 'Passed' }).Count
        adapterClassFailed = @($adapterClass | Where-Object { $_.outcome -eq 'Failed' }).Count
        notImplementedCount = @($outcomes | Where-Object { $_.message -match 'PluginHostProcessAdapter\.LaunchAsync is not implemented' }).Count
        unitOutcomes = $outcomes
    } | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $dest -Encoding utf8
}

Get-TrxSummary -TrxPath (Join-Path $out 'dotnet-adapter-filter.trx') -Name 'filter'
Get-TrxSummary -TrxPath (Join-Path $out 'dotnet-pluginintegration-all.trx') -Name 'all'

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
        p7 = @($taskObjs | Where-Object { $_.task -match '^P7' })
        p8 = @($taskObjs | Where-Object { $_.task -match '^P8' })
        p9 = @($taskObjs | Where-Object { $_.task -match '^P9' })
        p10 = @($taskObjs | Where-Object { $_.task -match '^P10' })
        p11 = @($taskObjs | Where-Object { $_.task -match '^P11' })
        tasks = $taskObjs
    } | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $Dest -Encoding utf8
}

Extract-TodoDone -Path (Join-Path $out 'todo-plan.txt') -Dest (Join-Path $out 'todo-plan-done-extract.json')
Extract-TodoDone -Path (Join-Path $out 'todo-pluginint.txt') -Dest (Join-Path $out 'todo-pluginint-done-extract.json')
Extract-TodoDone -Path (Join-Path $out 'todo-plan-client.txt') -Dest (Join-Path $out 'todo-plan-client-done-extract.json')
Extract-TodoDone -Path (Join-Path $out 'todo-pluginint-client.txt') -Dest (Join-Path $out 'todo-pluginint-client-done-extract.json')

$filterLog = Join-Path $out 'dotnet-adapter-filter.log'
$allLog = Join-Path $out 'dotnet-pluginintegration-all.log'
$console = [ordered]@{}
if (Test-Path $filterLog) {
    $t = Get-Content -LiteralPath $filterLog -Raw
    $console.filterFailed = if ($t -match 'Failed:\s+(\d+)') { $Matches[1] } else { $null }
    $console.filterPassed = if ($t -match 'Passed:\s+(\d+)') { $Matches[1] } else { $null }
    $console.filterSkipped = if ($t -match 'Skipped:\s+(\d+)') { $Matches[1] } else { $null }
    $console.filterTotal = if ($t -match 'Total:\s+(\d+)') { $Matches[1] } else { $null }
    $console.filterFailedBang = ($t -match 'Failed!')
    $console.filterPassedBang = ($t -match 'Passed!')
}
if (Test-Path $allLog) {
    $t = Get-Content -LiteralPath $allLog -Raw
    $console.allFailed = if ($t -match 'Failed:\s+(\d+)') { $Matches[1] } else { $null }
    $console.allPassed = if ($t -match 'Passed:\s+(\d+)') { $Matches[1] } else { $null }
    $console.allSkipped = if ($t -match 'Skipped:\s+(\d+)') { $Matches[1] } else { $null }
    $console.allTotal = if ($t -match 'Total:\s+(\d+)') { $Matches[1] } else { $null }
    $console.allFailedBang = ($t -match 'Failed!')
    $console.allPassedBang = ($t -match 'Passed!')
}
$console | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $out 'dotnet-console-counts.json') -Encoding utf8

Write-Output 'PARSE_DONE'
