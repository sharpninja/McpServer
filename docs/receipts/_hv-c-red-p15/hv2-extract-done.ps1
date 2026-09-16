#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$out = 'F:\GitHub\McpServer\docs\receipts\_hv-c-red-p15\hv2'

function Get-FirstDone([string]$text) {
    $m = [regex]::Match($text, '(?m)^\s+done:\s*(true|false)\s*$')
    if ($m.Success) { return $m.Groups[1].Value }
    return 'MISSING'
}

function Get-TaskDone([string]$text, [string]$needle) {
    $idx = $text.IndexOf($needle, [StringComparison]::Ordinal)
    if ($idx -lt 0) { return 'MISSING' }
    $slice = $text.Substring($idx, [Math]::Min(400, $text.Length - $idx))
    $m = [regex]::Match($slice, 'done:\s*(true|false)')
    if ($m.Success) { return $m.Groups[1].Value }
    return 'MISSING'
}

$plan = Get-Content -LiteralPath (Join-Path $out 'todo-plan.txt') -Raw
$pluginint = Get-Content -LiteralPath (Join-Path $out 'todo-pluginint.txt') -Raw
$obj = [ordered]@{
    PlanTopDone = Get-FirstDone $plan
    PluginIntTopDone = Get-FirstDone $pluginint
    PlanCombinedC14C15Done = Get-TaskDone $plan 'C P14 red + hostile then green; P15 red + hostile then green.'
    PluginIntP15Done = Get-TaskDone $pluginint 'P15 [Red] Add failsafe assertions'
    PluginIntP14Done = Get-TaskDone $pluginint 'P14 [Red] Inject PLUGIN_ROOT_OVERRIDE'
    PluginIntP16Done = Get-TaskDone $pluginint 'P16 [Red] Add companion AiTheory'
}
$json = $obj | ConvertTo-Json
Set-Content -LiteralPath (Join-Path $out 'todo-done-extract.json') -Value $json -Encoding utf8
Write-Output $json
