$ErrorActionPreference = 'Stop'
$root = 'F:\GitHub\McpServer\.worktrees\sessionlife-p2-contracts-5cb2\build'
Get-ChildItem -LiteralPath $root -Filter '*.cs' -File | ForEach-Object {
  $lines = [System.IO.File]::ReadAllLines($_.FullName)
  for ($i = 0; $i -lt $lines.Length; $i++) {
    if ($lines[$i] -match 'EnumeratePolicySources|git-tracked|Untracked|ls-files') {
      Write-Output ("{0}:{1}: {2}" -f $_.Name, ($i + 1), $lines[$i].Trim())
    }
  }
}
