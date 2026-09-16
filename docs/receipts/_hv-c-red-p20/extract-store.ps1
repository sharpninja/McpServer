#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$out = 'F:\GitHub\McpServer\docs\receipts\_hv-c-red-p20'

function Get-YamlScalar {
    param([string]$Path, [string]$Key)
    $line = Select-String -LiteralPath $Path -Pattern ("^\s+" + [regex]::Escape($Key) + ":\s*(.*)$") | Select-Object -First 1
    if ($null -eq $line) { return $null }
    return $line.Matches[0].Groups[1].Value.Trim().Trim("'").Trim('"')
}

function Get-TaskDoneFlags {
    param([string]$Path, [string]$Needle)
    $lines = Get-Content -LiteralPath $Path
    $hits = [System.Collections.Generic.List[object]]::new()
    for ($i = 0; $i -lt $lines.Count; $i++) {
        if ($lines[$i] -match [regex]::Escape($Needle)) {
            $done = $null
            for ($j = $i; $j -lt [Math]::Min($i + 8, $lines.Count); $j++) {
                if ($lines[$j] -match '^\s+done:\s*(true|false)') {
                    $done = $Matches[1]
                    break
                }
            }
            $hits.Add([ordered]@{ line = ($i + 1); text = $lines[$i].Trim(); done = $done })
        }
    }
    return @($hits)
}

$planPath = Join-Path $out 'todo-plan-client.txt'
$pluginintPath = Join-Path $out 'todo-pluginint-client.txt'
$hygienePath = Join-Path $out 'todo-hygiene-client.txt'
$reqTestPath = Join-Path $out 'req-test.txt'
$reqFrPath = Join-Path $out 'req-fr.txt'
$reqTrPath = Join-Path $out 'req-tr.txt'
$reqMapPath = Join-Path $out 'req-map.txt'

$planTasks = @(
    (Get-TaskDoneFlags -Path $planPath -Needle 'C P20 named tests')
    (Get-TaskDoneFlags -Path $planPath -Needle 'P20 red tests')
    (Get-TaskDoneFlags -Path $pluginintPath -Needle 'P20 [Deployment]')
)

[ordered]@{
    planId = Get-YamlScalar -Path $planPath -Key 'id'
    planDone = Get-YamlScalar -Path $planPath -Key 'done'
    pluginintId = Get-YamlScalar -Path $pluginintPath -Key 'id'
    pluginintDone = Get-YamlScalar -Path $pluginintPath -Key 'done'
    hygieneId = Get-YamlScalar -Path $hygienePath -Key 'id'
    hygieneDone = Get-YamlScalar -Path $hygienePath -Key 'done'
    hygieneRemaining = Get-YamlScalar -Path $hygienePath -Key 'remaining'
    p20TaskHits = $planTasks
} | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $out 'todo-done-extract.json') -Encoding utf8

$acLines = @(Select-String -LiteralPath $reqTestPath -Pattern 'AC\s*5|acceptance|UpdateService|deploy|native suite|operator approval|sanitized' -CaseSensitive:$false)
$frLines = @(Select-String -LiteralPath $reqFrPath -Pattern 'AC\s*5|UpdateService|deploy|operator approval|sanitized' -CaseSensitive:$false)
$trLines = @(Select-String -LiteralPath $reqTrPath -Pattern 'AC\s*5|UpdateService|deploy|operator approval|sanitized' -CaseSensitive:$false)
$mapLines = @(Select-String -LiteralPath $reqMapPath -Pattern 'TEST-MCP-PLUGININT-001|FR-MCP-PLUGININT-001|TR-MCP-PLUGININT-001')

[ordered]@{
    testId = Get-YamlScalar -Path $reqTestPath -Key 'id'
    testIsSatisfied = Get-YamlScalar -Path $reqTestPath -Key 'isSatisfied'
    testStatus = Get-YamlScalar -Path $reqTestPath -Key 'status'
    frId = Get-YamlScalar -Path $reqFrPath -Key 'id'
    frIsSatisfied = Get-YamlScalar -Path $reqFrPath -Key 'isSatisfied'
    trId = Get-YamlScalar -Path $reqTrPath -Key 'id'
    trIsSatisfied = Get-YamlScalar -Path $reqTrPath -Key 'isSatisfied'
    testHits = @($acLines | Select-Object -First 40 | ForEach-Object { [ordered]@{ line = $_.LineNumber; text = $_.Line.Trim() } })
    frHits = @($frLines | Select-Object -First 20 | ForEach-Object { [ordered]@{ line = $_.LineNumber; text = $_.Line.Trim() } })
    trHits = @($trLines | Select-Object -First 20 | ForEach-Object { [ordered]@{ line = $_.LineNumber; text = $_.Line.Trim() } })
    mapHits = @($mapLines | Select-Object -First 20 | ForEach-Object { [ordered]@{ line = $_.LineNumber; text = $_.Line.Trim() } })
} | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $out 'req-extract.json') -Encoding utf8

Write-Output 'EXTRACT_DONE'
