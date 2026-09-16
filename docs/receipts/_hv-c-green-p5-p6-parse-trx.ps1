#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'

$out = 'F:\GitHub\McpServer\docs\receipts\_hv-c-green-p5-p6'

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
        $outcomes += [ordered]@{
            name = Get-Attr $u 'testName'
            outcome = Get-Attr $u 'outcome'
            duration = Get-Attr $u 'duration'
            message = $(if ($msgNode) { [string]$msgNode.InnerText } else { $null })
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
        adapterNames = @($outcomes | Where-Object { $_.name -match 'Adapter_CapturesExecutable' } | ForEach-Object { $_.name })
        unitOutcomes = $outcomes
    } | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $dest -Encoding utf8
}

Get-TrxSummary -TrxPath (Join-Path $out 'dotnet-fixture-filter.trx') -Name 'filter'
Get-TrxSummary -TrxPath (Join-Path $out 'dotnet-pluginintegration-all.trx') -Name 'all'

$verPath = 'F:\GitHub\mcpserver-grok-plugin\.version'
$pjPath = 'F:\GitHub\mcpserver-grok-plugin\.grok-plugin\plugin.json'
$verObj = [ordered]@{
    VersionFile = if (Test-Path $verPath) { (Get-Content -LiteralPath $verPath -Raw).Trim() } else { $null }
    PluginJsonVersion = $null
}
if (Test-Path $pjPath) {
    $pj = Get-Content -LiteralPath $pjPath -Raw | ConvertFrom-Json
    $verObj.PluginJsonVersion = $pj.version
}
$verObj | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $out 'plugin-version.json') -Encoding utf8

$plan = Get-Content -LiteralPath (Join-Path $out 'todo-plan.txt') -Raw
$pluginint = Get-Content -LiteralPath (Join-Path $out 'todo-pluginint.txt') -Raw
$planClient = Get-Content -LiteralPath (Join-Path $out 'todo-plan-client.txt') -Raw
$pluginintClient = Get-Content -LiteralPath (Join-Path $out 'todo-pluginint-client.txt') -Raw

function Get-YamlDone([string]$text) {
    if ($text -match '(?m)^\s+done:\s+(true|false)\s*$') { return $Matches[1] }
    return 'missing'
}

[ordered]@{
    PlanWorkflowDone = Get-YamlDone $plan
    PlanClientDone = Get-YamlDone $planClient
    PluginintWorkflowDone = Get-YamlDone $pluginint
    PluginintClientDone = Get-YamlDone $pluginintClient
    PluginintP5Done = [bool]($pluginint -match '(?s)P5 \[Red\].*?done: false')
    PluginintP6DoneFalse = [bool]($pluginint -match '(?s)P6 \[Green\].*?done: false')
    PluginintP7DoneFalse = [bool]($pluginint -match '(?s)P7 \[Red\].*?done: false')
    PlanCTaskDoneFalse = [bool]($plan -match '(?s)P5 red \+ hostile then P6\.\s+done: false')
} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $out 'todo-done-extract.json') -Encoding utf8

Write-Output 'PARSE_DONE'
