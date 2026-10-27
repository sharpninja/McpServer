$ErrorActionPreference = 'Stop'
$nlog = 'F:\GitHub\McpServer\.worktrees\sessionlife-p2-contracts-5cb2\TestResults\p2-unit-legion-20261002T220847Z\logs\nuke-test.log'
$lines = [System.IO.File]::ReadAllLines($nlog)
$hits = New-Object System.Collections.Generic.List[int]
for ($i = 0; $i -lt $lines.Length; $i++) {
  if ($lines[$i] -match 'EnsureAgentSessionHeaderColumns_MissingColumns|PendingMigration|Failed McpServer.Support') {
    $hits.Add($i)
  }
}
Write-Output ("HITS=" + $hits.Count)
foreach ($i in $hits) {
  $from = [Math]::Max(0, $i - 2)
  $to = [Math]::Min($lines.Length - 1, $i + 25)
  Write-Output ("----- around " + ($i + 1) + " -----")
  for ($j = $from; $j -le $to; $j++) { Write-Output $lines[$j] }
}
