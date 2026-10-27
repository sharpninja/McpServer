$ErrorActionPreference = 'Stop'
$wt = 'F:\GitHub\McpServer\.worktrees\sessionlife-p2-contracts-5cb2'
$audit = Get-Content -LiteralPath (Join-Path $wt 'docs\receipts\hv\20261002T203600Z-trace-audit.json') -Raw | ConvertFrom-Json
Write-Output ('P2ROWS=' + @($audit.p2Rows).Count)
$ids = @(
    'FR-MCP-SESSIONLIFE-002-AC002',
    'TR-MCP-SESSIONLIFE-002-AC003',
    'TEST-MCP-SESSIONLIFE-002-AC001',
    'TEST-MCP-SESSIONLIFE-002-AC004',
    'FR-MCP-170-AC002',
    'FR-MCP-170-AC003',
    'AC-FR-MCP-SESSIONLOGCTX-001-002',
    'AC-FR-MCP-SESSIONLOGCTX-001-003',
    'FR-MCP-REPL-009-AC001',
    'FR-MCP-REPL-009-AC002',
    'FR-MCP-REPL-009-AC003',
    'FR-MCP-REPL-009-AC004'
)
foreach ($row in @($audit.p2Rows)) {
    $id = [string]$row.id
    if (-not $id) { $id = [string]$row.criterionId }
    if ($ids -notcontains $id) { continue }
    Write-Output ('==== ' + $id)
    $row.PSObject.Properties | ForEach-Object {
        $v = $_.Value
        if ($null -eq $v) { $text = '' }
        elseif ($v -is [string]) { $text = $v }
        else { $text = ($v | ConvertTo-Json -Compress -Depth 6) }
        if ($text.Length -gt 800) { $text = $text.Substring(0, 800) }
        Write-Output ($_.Name + '=' + $text)
    }
}
Write-Output '--- TRACE COUNTS ---'
$audit.traceCounts | ConvertTo-Json -Depth 4
