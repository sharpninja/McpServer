$ErrorActionPreference = 'Stop'
$wt = 'F:\GitHub\McpServer\.worktrees\sessionlife-p2-contracts-5cb2'
$root = Join-Path $wt 'plugins\core'
Get-ChildItem -LiteralPath $root -Recurse -Include '*.ps1' -File |
  Where-Object { $_.FullName -notmatch '\\(\.staged-plugin|bin|obj)\\' -and $_.Name -match 'hook|dialog|prompt' } |
  ForEach-Object { $_.FullName.Substring($wt.Length) }
Write-Output '===== appendDialog in lib-ps ====='
Get-ChildItem (Join-Path $root 'lib-ps') -Filter '*.ps1' -File | ForEach-Object {
  $hits = Select-String -LiteralPath $_.FullName -Pattern 'appendDialog|turn-opened|hookSpecificOutput' | Select-Object -First 3
  foreach ($h in $hits) { Write-Output ($_.Name + ':' + $h.LineNumber + ':' + $h.Line.Trim()) }
}
