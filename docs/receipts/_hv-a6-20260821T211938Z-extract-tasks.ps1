$ErrorActionPreference = 'Stop'
$p = 'C:\Users\kingd\.grok\sessions\F%3A%5CGitHub%5CMcpServer\01a02630-ef89-7530-9b4e-80c2a879093f\mcp\call-3ef05f9f-e3a3-4369-9375-06a301ae1610-44.json'
$out = 'F:\GitHub\McpServer\docs\receipts\_hv-a6-20260821T211938Z'
$j = Get-Content -LiteralPath $p -Raw | ConvertFrom-Json
$i = 0
$lines = foreach ($t in @($j.ImplementationTasks)) {
    $i++
    '{0}`t{1}`t{2}' -f $i, $t.Done, $t.Task
}
$lines | Set-Content (Join-Path $out 'plan-todo-tasks.tsv') -Encoding utf8
$done = @($j.ImplementationTasks | Where-Object { $_.Done -eq $true } | ForEach-Object { $_.Task })
$done | Set-Content (Join-Path $out 'plan-todo-tasks-done.txt') -Encoding utf8
Write-Output ('doneCount=' + $done.Count)
$done | ForEach-Object { Write-Output $_ }
