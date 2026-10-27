$ErrorActionPreference = 'Stop'
$path = 'F:\GitHub\McpServer\.worktrees\sessionlife-p2-contracts-5cb2\plugins\core\lib-ps\repl-invoke.ps1'
$lines = [System.IO.File]::ReadAllLines($path)
for ($i = 0; $i -lt $lines.Length; $i++) {
  if ($lines[$i] -match "degraded|failsafePath|current-turn") {
    if ($lines[$i] -match 'Write-McpYamlObject|degraded\s*=') {
      Write-Output ("{0}: {1}" -f ($i + 1), $lines[$i].Trim())
    }
  }
}
