$ErrorActionPreference = 'Stop'
$path = 'F:\GitHub\McpServer\.worktrees\sessionlife-p2-contracts-5cb2\build\SessionLifeUnitGate.cs'
$lines = [System.IO.File]::ReadAllLines($path)
for ($i = 0; $i -lt $lines.Length; $i++) {
  if ($lines[$i] -match 'EnumeratePolicySources|relevant-untracked|git ls-files|IsRelevant') {
    Write-Output ("{0}: {1}" -f ($i + 1), $lines[$i])
  }
}
