$ErrorActionPreference = 'Stop'
$p = 'C:\Users\kingd\.grok\sessions\F%3A%5CGitHub%5CMcpServer\01a02630-ef89-7530-9b4e-80c2a879093f\mcp\call-3ef05f9f-e3a3-4369-9375-06a301ae1610-44.json'
$out = 'F:\GitHub\McpServer\docs\receipts\_hv-a6-20260821T211938Z'
New-Item -ItemType Directory -Force -Path $out | Out-Null
$j = Get-Content -LiteralPath $p -Raw | ConvertFrom-Json
$fr = @($j.FunctionalRequirements)
$tr = @($j.TechnicalRequirements)
$tasks = @($j.ImplementationTasks)
$obj = [pscustomobject]@{
    Id = $j.Id
    Done = [bool]$j.Done
    DoneSummary = $j.DoneSummary
    FrCount = $fr.Count
    TrCount = $tr.Count
    TaskCount = $tasks.Count
    TasksDone = @($tasks | Where-Object { $_.Done -eq $true }).Count
    FunctionalRequirements = $fr
    TechnicalRequirements = $tr
}
$obj | ConvertTo-Json -Depth 6 | Set-Content (Join-Path $out 'plan-todo-extract.json') -Encoding utf8
$fr | Set-Content (Join-Path $out 'plan-todo-fr.txt') -Encoding utf8
$tr | Set-Content (Join-Path $out 'plan-todo-tr.txt') -Encoding utf8
Write-Output ('PLAN done=' + $j.Done + ' FR=' + $fr.Count + ' TR=' + $tr.Count)
