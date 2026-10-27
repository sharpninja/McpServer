$ErrorActionPreference = 'Stop'
$p = 'F:\GitHub\McpServer\.worktrees\sessionlife-p2-contracts-5cb2\docs\receipts\sessionlife-completion\20260928-p0-r3\acceptance-manifest.json'
$lines = [System.IO.File]::ReadAllLines($p)
$ids = @('FR-MCP-170-AC002','FR-MCP-SESSIONLIFE-002-AC002','TEST-MCP-SESSIONLIFE-002-AC004')
for ($i = 0; $i -lt $lines.Length; $i++) {
  foreach ($id in $ids) {
    if ($lines[$i] -match [regex]::Escape($id)) {
      $start = [Math]::Max(0, $i - 2)
      $end = [Math]::Min($lines.Length - 1, $i + 35)
      Write-Output "----- $id at $($i+1) -----"
      for ($j = $start; $j -le $end; $j++) { Write-Output ("{0}|{1}" -f ($j+1), $lines[$j]) }
    }
  }
}
