$ErrorActionPreference = 'Stop'
$p = 'F:\GitHub\McpServer\.worktrees\sessionlife-p2-contracts-5cb2\docs\receipts\sessionlife-completion\20260928-p0-r3\acceptance-manifest.json'
$rawText = [System.IO.File]::ReadAllText($p)
$parts = [string[]]($rawText -split "`r?`n", -1)
Write-Output ("COUNT=" + $parts.Count)
$hits = @()
for ($i = 0; $i -lt $parts.Count; $i++) {
  if ($parts[$i] -and $parts[$i].Contains('FR-MCP-REPL-009-AC003')) { $hits += $i }
}
Write-Output ("HITS=" + ($hits -join ','))
if ($hits.Count -gt 0) {
  $i = $hits[0]
  Write-Output ("LINE=[" + $parts[$i] + "]")
  $expect = '"acceptanceCriterionId": "FR-MCP-REPL-009-AC003",'
  Write-Output ("EQ=" + ($parts[$i].Trim() -eq $expect))
  $codes = ($parts[$i].Trim().ToCharArray() | Select-Object -First 12 | ForEach-Object { [int]$_ }) -join ','
  Write-Output ("CODES=" + $codes)
}
