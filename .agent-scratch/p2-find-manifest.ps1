$ErrorActionPreference = 'Stop'
$wt = 'F:\GitHub\McpServer\.worktrees\sessionlife-p2-contracts-5cb2'
Get-ChildItem -LiteralPath $wt -Recurse -Filter 'acceptance-manifest.json' -File |
    Where-Object { $_.FullName -notmatch '\\(bin|obj|TestResults|\.git)\\' } |
    ForEach-Object { Write-Output ($_.FullName + ' ' + $_.Length) }
