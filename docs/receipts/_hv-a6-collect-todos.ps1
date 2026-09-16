#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$out = 'F:\GitHub\McpServer\docs\receipts\_hv-a6-20260821T213854Z'
$base = 'http://PAYTON-LEGION2:7147'
$headers = @{
    'X-Api-Key' = 'tMxsITg8_OfmC8NrwlwMMCEYkr6lD0_A0zHjGU8egjg'
    'X-Workspace-Path' = 'F:\GitHub\McpServer'
}
$ids = @(
    'PLAN-PLUGINHANDOFF-001',
    'MCP-HOSTILEREVIEW-001',
    'MCP-WORKSPACEHYGIENE-002',
    'MCP-WIKIEXPORT-001',
    'MCP-PLUGINCORE-004',
    'BUG-TRIAGE-170'
)
$lines = New-Object System.Collections.Generic.List[string]
$lines.Add('Id`tDone`tFrCount`tTrCount`tFr`tTr`tRemainingPreview')
foreach ($id in $ids) {
    $uri = "$base/mcpserver/todo/$([uri]::EscapeDataString($id))"
    $resp = Invoke-WebRequest -Uri $uri -Headers $headers -UseBasicParsing
    $safe = ($id -replace '[^A-Za-z0-9\-]', '_')
    $resp.Content | Set-Content -LiteralPath (Join-Path $out ("todo-{0}.json" -f $safe)) -Encoding utf8
    $j = $resp.Content | ConvertFrom-Json
    $names = @($j.PSObject.Properties.Name)
    $fr = @()
    $tr = @()
    if ($names -contains 'FunctionalRequirements' -and $null -ne $j.FunctionalRequirements) { $fr = @($j.FunctionalRequirements) }
    if ($names -contains 'TechnicalRequirements' -and $null -ne $j.TechnicalRequirements) { $tr = @($j.TechnicalRequirements) }
    $rem = ''
    if ($names -contains 'Remaining' -and $null -ne $j.Remaining) { $rem = [string]$j.Remaining }
    $lines.Add(('{0}`t{1}`t{2}`t{3}`t{4}`t{5}`t{6}' -f $id, $j.Done, $fr.Count, $tr.Count, ($fr -join ','), ($tr -join ','), ($rem.Substring(0, [Math]::Min(220, $rem.Length)) -replace "`r|`n", ' ')))
}
$lines -join "`n" | Set-Content (Join-Path $out 'todo-link-summary.tsv') -Encoding utf8
Write-Output ($lines -join "`n")
