#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$out = 'F:\GitHub\McpServer\docs\receipts\_hv-a6-20260821T213854Z'
$planPath = 'C:\Users\kingd\.grok\sessions\F%3A%5CGitHub%5CMcpServer\01a0263e-eb55-7f53-8a85-ef1033c35a2c\mcp\call-126046f4-c106-4ce5-abaa-761560e48e50-67.json'
$raw = Get-Content -LiteralPath $planPath -Raw
$wrap = $raw | ConvertFrom-Json
$j = $null
$names = @($wrap.PSObject.Properties.Name)
if ($names -contains 'Id') { $j = $wrap }
elseif ($names -contains 'text') { $j = $wrap.text | ConvertFrom-Json }
elseif ($names -contains 'result') {
    $t = $wrap.result.content[0].text
    $j = $t | ConvertFrom-Json
}
else { throw 'unparsed plan todo envelope' }
$i = 0
$lines = foreach ($t in @($j.ImplementationTasks)) {
    $i++
    '{0}`t{1}`t{2}' -f $i, $t.Done, (($t.Task -replace "`r|`n", ' '))
}
$lines | Set-Content (Join-Path $out 'plan-todo-tasks.tsv') -Encoding utf8
$fr = @($j.FunctionalRequirements)
$tr = @($j.TechnicalRequirements)
@(
    ('Done={0}' -f $j.Done)
    ('DoneSummary={0}' -f $j.DoneSummary)
    ('FrCount={0}' -f $fr.Count)
    ('TrCount={0}' -f $tr.Count)
    ('Fr={0}' -f ($fr -join ','))
    ('Tr={0}' -f ($tr -join ','))
) | Set-Content (Join-Path $out 'plan-todo-meta.txt') -Encoding utf8
Write-Output ('Done=' + $j.Done)
Write-Output ('FrCount=' + $fr.Count)
Write-Output ('TrCount=' + $tr.Count)
Write-Output 'TASKS'
$lines
Write-Output 'FR'
$fr
Write-Output 'TR'
$tr
