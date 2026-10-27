$ErrorActionPreference = 'Stop'
$src = 'F:\GitHub\McpServer\.worktrees\sessionlife-p2-contracts-5cb2\plugins\core\lib-ps\repl-invoke.ps1'
$i = 0
$hits = @()
foreach ($line in [System.IO.File]::ReadLines($src)) {
  $i++
  if ($line -match 'function Invoke-WorkflowCompleteTurn|function Invoke-WorkflowFailTurn|LastReplPersistenceDetails|degraded') {
    $hits += "{0}: {1}" -f $i, $line.Trim()
  }
}
$hits | Select-Object -First 80
Write-Output '---- count ----'
Write-Output $hits.Count
