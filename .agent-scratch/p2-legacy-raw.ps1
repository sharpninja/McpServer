$ErrorActionPreference = 'Stop'
$p = 'F:\GitHub\McpServer\.worktrees\sessionlife-p2-contracts-5cb2\docs\receipts\sessionlife-completion\20260928-p0-r3\acceptance-manifest.json'
$lines = [System.IO.File]::ReadAllLines($p)
for ($i = 0; $i -lt $lines.Length; $i++) {
  if ($lines[$i] -eq '      "acceptanceCriterionId": "FR-MCP-170-AC002",') {
    for ($j = $i; $j -lt [Math]::Min($i + 45, $lines.Length); $j++) {
      Write-Output ("{0}|{1}" -f ($j+1), $lines[$j])
    }
    break
  }
}
Write-Output '==== revision ===='
for ($i = 0; $i -lt $lines.Length; $i++) {
  if ($lines[$i] -match '"p2MappingRevision"') {
    for ($j = $i; $j -lt [Math]::Min($i + 12, $lines.Length); $j++) {
      Write-Output ("{0}|{1}" -f ($j+1), $lines[$j])
    }
    break
  }
}
