$ErrorActionPreference = 'Continue'
$wt = 'F:\GitHub\McpServer\.worktrees\sessionlife-p2-contracts-5cb2'
Write-Output ('HEAD=' + (git -C $wt rev-parse HEAD))
Write-Output ('BRANCH=' + (git -C $wt rev-parse --abbrev-ref HEAD))
Write-Output '=== PESTER HITS ==='
Get-ChildItem -LiteralPath (Join-Path $wt 'plugins\core\test-fixtures\pester') -Filter '*.ps1' |
    Select-String -Pattern 'covers primary queued|codex exposes exactly one|Invoke-CodexMcpPlugin' |
    ForEach-Object { $_.Filename + ':' + $_.LineNumber + ':' + $_.Line.Trim() }
Write-Output '=== GROK TEST ==='
Get-ChildItem -LiteralPath (Join-Path $wt 'tests') -Recurse -Filter '*CliBrainSlot*' -File |
    ForEach-Object { $_.FullName }
Write-Output '=== CODEX PLUGIN ==='
$candidates = @(
    'F:\GitHub\McpServer\.worktrees\mcpserver-codex-plugin',
    'F:\GitHub\mcpserver-codex-plugin',
    'F:\github\mcpserver-codex-plugin'
)
foreach ($c in $candidates) {
    Write-Output ('PATH=' + $c + ' EXISTS=' + (Test-Path -LiteralPath $c))
    $script = Join-Path $c 'Invoke-CodexMcpPlugin.ps1'
    Write-Output ('SCRIPT=' + $script + ' EXISTS=' + (Test-Path -LiteralPath $script))
}
Write-Output '=== WORKTREES ==='
git -C 'F:\GitHub\McpServer' worktree list
