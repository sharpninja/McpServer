#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'

$out = 'F:\GitHub\McpServer\docs\receipts\_hv-c-red-p16'

function Get-YamlScalar {
    param([string]$Text, [string]$Key)
    if ($Text -match "(?m)^\s*${Key}:\s*(.+)$") { return $Matches[1].Trim() }
    return $null
}

$planText = Get-Content -LiteralPath (Join-Path $out 'todo-plan.txt') -Raw
$pluginintText = Get-Content -LiteralPath (Join-Path $out 'todo-pluginint.txt') -Raw
$planClient = Get-Content -LiteralPath (Join-Path $out 'todo-plan-client.txt') -Raw
$pluginintClient = Get-Content -LiteralPath (Join-Path $out 'todo-pluginint-client.txt') -Raw

function Get-TaskFlags {
    param([string]$Text)
    $flags = [ordered]@{}
    $matches = [regex]::Matches($Text, '(?ms)-\s+task:\s*(.+?)\n\s+done:\s*(true|false)')
    foreach ($m in $matches) {
        $task = $m.Groups[1].Value.Trim().Trim("'").Trim('"')
        $flags[$task] = [bool]::Parse($m.Groups[2].Value)
    }
    return $flags
}

$planTasks = Get-TaskFlags $planText
$intTasks = Get-TaskFlags $pluginintText

[ordered]@{
    PlanId = Get-YamlScalar $planText 'id'
    PlanDone = Get-YamlScalar $planText 'done'
    PlanNote = Get-YamlScalar $planText 'note'
    PlanTaskCount = $planTasks.Count
    PlanTasksDoneTrue = @($planTasks.GetEnumerator() | Where-Object { $_.Value } | ForEach-Object { $_.Key })
    PlanTasksContainingP16 = @($planTasks.GetEnumerator() | Where-Object { $_.Key -match 'P16' } | ForEach-Object { [ordered]@{ task = $_.Key; done = $_.Value } })
    PlanTasksContainingC = @($planTasks.GetEnumerator() | Where-Object { $_.Key -match 'P16|PLUGININT|Phase C' } | ForEach-Object { [ordered]@{ task = $_.Key; done = $_.Value } })
    PluginintId = Get-YamlScalar $pluginintText 'id'
    PluginintDone = Get-YamlScalar $pluginintText 'done'
    PluginintP16 = @($intTasks.GetEnumerator() | Where-Object { $_.Key -match '^P16' } | ForEach-Object { [ordered]@{ task = $_.Key; done = $_.Value } })
    PluginintP15 = @($intTasks.GetEnumerator() | Where-Object { $_.Key -match '^P15' } | ForEach-Object { [ordered]@{ task = $_.Key; done = $_.Value } })
    PluginintP17 = @($intTasks.GetEnumerator() | Where-Object { $_.Key -match '^P17' } | ForEach-Object { [ordered]@{ task = $_.Key; done = $_.Value } })
    AllIntDoneTrue = @($intTasks.GetEnumerator() | Where-Object { $_.Value } | ForEach-Object { $_.Key })
    PlanClientDone = Get-YamlScalar $planClient 'done'
    PluginintClientDone = Get-YamlScalar $pluginintClient 'done'
} | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $out 'todo-extract.json') -Encoding utf8

function Get-AcBlock {
    param([string]$Text)
    $acs = @()
    $matches = [regex]::Matches($Text, '(?ms)-\s+id:\s*(ac-\d+)\r?\n\s+text:\s*(.+?)\r?\n\s+isSatisfied:\s*(true|false)')
    foreach ($m in $matches) {
        $acs += [ordered]@{
            id = $m.Groups[1].Value
            text = $m.Groups[2].Value.Trim()
            isSatisfied = [bool]::Parse($m.Groups[3].Value)
        }
    }
    return $acs
}

$fr = Get-Content -LiteralPath (Join-Path $out 'req-fr.txt') -Raw
$tr = Get-Content -LiteralPath (Join-Path $out 'req-tr.txt') -Raw
$test = Get-Content -LiteralPath (Join-Path $out 'req-test.txt') -Raw
$map = Get-Content -LiteralPath (Join-Path $out 'req-map.txt') -Raw

[ordered]@{
    FrId = Get-YamlScalar $fr 'id'
    FrStatus = Get-YamlScalar $fr 'status'
    FrAc = @(Get-AcBlock $fr)
    TrId = Get-YamlScalar $tr 'id'
    TrStatus = Get-YamlScalar $tr 'status'
    TrAc = @(Get-AcBlock $tr)
    TestId = Get-YamlScalar $test 'id'
    TestStatus = Get-YamlScalar $test 'status'
    TestAc = @(Get-AcBlock $test)
    MapSnippet = if ($map.Length -gt 4000) { $map.Substring(0, 4000) } else { $map }
    MapHasFrTr = [bool]($map -match 'TR-MCP-PLUGININT-001')
    MapHasFrTest = [bool]($map -match 'TEST-MCP-PLUGININT-001')
} | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $out 'req-extract.json') -Encoding utf8

$todoYamlStatus = git -C 'F:\GitHub\McpServer' status --porcelain -- docs/Project/TODO.yaml docs/todo.yaml
[ordered]@{ TodoYamlPorcelain = [string]$todoYamlStatus; TimestampUtc = [DateTime]::UtcNow.ToString('o') } |
    ConvertTo-Json | Set-Content -LiteralPath (Join-Path $out 'todo-yaml-git-status-after.json') -Encoding utf8

Write-Output 'EXTRACT_DONE'
