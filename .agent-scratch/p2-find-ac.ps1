$ErrorActionPreference = 'Stop'
$p = 'F:\GitHub\McpServer\.worktrees\sessionlife-p2-contracts-5cb2\docs\receipts\sessionlife-completion\20260928-p0-r3\acceptance-manifest.json'
$lines = [System.IO.File]::ReadAllLines($p)
$ids = @('FR-MCP-REPL-009-AC001','FR-MCP-REPL-009-AC002','FR-MCP-REPL-009-AC003','FR-MCP-REPL-009-AC004','AC-FR-MCP-SESSIONLOGCTX-001-002','AC-FR-MCP-SESSIONLOGCTX-001-003','TR-MCP-SESSIONLIFE-002-AC003','TEST-MCP-SESSIONLIFE-002-AC001')
foreach ($id in $ids) {
  $hits = @()
  for ($i = 0; $i -lt $lines.Length; $i++) {
    if ($lines[$i].Contains($id)) { $hits += ($i+1).ToString() + ':' + $lines[$i] }
  }
  Write-Output ("==== $id hits=$($hits.Count)")
  $hits | Select-Object -First 4 | ForEach-Object { Write-Output $_ }
}
