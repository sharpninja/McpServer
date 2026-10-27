$ErrorActionPreference = 'Stop'
$wt = 'F:\GitHub\McpServer\.worktrees\sessionlife-p2-contracts-5cb2'
$roots = @(
  (Join-Path $wt 'plugins\core'),
  (Join-Path $wt 'src')
)
$patterns = @('opened-degraded', 'openedDegraded', 'recoveryArtifactPath', 'Prompt-hook', 'prompt-hook', 'UserPromptSubmit')
foreach ($pat in $patterns) {
  Write-Output "===== $pat ====="
  Get-ChildItem -LiteralPath $roots -Recurse -Include '*.ps1','*.cs','*.md' -File |
    Where-Object { $_.FullName -notmatch '\\(bin|obj|\.git)\\' } |
    Select-String -Pattern $pat -SimpleMatch |
    Select-Object -First 12 |
    ForEach-Object { Write-Output ($_.Path.Substring($wt.Length) + ':' + $_.LineNumber) }
}
