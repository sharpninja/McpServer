$ErrorActionPreference = 'Continue'
$wt = 'F:\GitHub\McpServer\.worktrees\sessionlife-p2-contracts-5cb2'
Write-Output '=== ENV VAR REFS ==='
Get-ChildItem -LiteralPath $wt -Recurse -Include *.ps1,*.cs,*.md -File -ErrorAction SilentlyContinue |
    Where-Object { $_.FullName -notmatch '\\(bin|obj|TestResults|\.git)\\' } |
    Select-String -Pattern 'MCP_NUKE_POWERSHELL_BOOTSTRAPPED' -SimpleMatch |
    Select-Object -First 20 |
    ForEach-Object { $_.Path.Replace($wt, '') + ':' + $_.LineNumber }
Write-Output '=== ASTRA LAUNCH HITS ==='
$hv = Join-Path $wt 'docs\receipts\hv'
Get-ChildItem -LiteralPath $hv -Filter '*20261001T113858Z*' -File -ErrorAction SilentlyContinue |
    ForEach-Object { $_.Name + ' ' + $_.Length }
Write-Output '=== HANDOFF ASTRA LINES ==='
$handoff = Join-Path $wt 'docs\handoffs\sessionlife-p2-contracts-pr72-20261001T113858Z.md'
Select-String -LiteralPath $handoff -Pattern 'codex|astra|gpt-6|xhigh|request.jsonl' |
    Select-Object -First 30 |
    ForEach-Object { $_.LineNumber.ToString() + ':' + $_.Line.Trim() }
