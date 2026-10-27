$ErrorActionPreference = 'Stop'
$p = 'F:\GitHub\McpServer\.worktrees\sessionlife-p2-contracts-5cb2\tests\Build.Tests\SessionLifeUnitGateValidatorTests.cs'
$i = 0
foreach ($line in [System.IO.File]::ReadLines($p)) {
  $i++
  if ($line -match 'public void Validate_|Theory|Fact') {
    Write-Output ("{0}: {1}" -f $i, $line.Trim())
  }
}
