$ErrorActionPreference = 'Continue'
$wt = 'F:\GitHub\McpServer\.worktrees\sessionlife-p2-contracts-5cb2'
Get-ChildItem -LiteralPath (Join-Path $wt 'src') -Recurse -Include *.cs -File |
    Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' } |
    Select-String -Pattern 'GROK_PLUGIN_ROOT|GROK_HOME|GROK_SUBAGENTS' |
    ForEach-Object { $_.Path.Replace($wt, '') + ':' + $_.LineNumber + ':' + $_.Line.Trim() }
Write-Output '=== STDOUT ASSERTIONS ==='
Select-String -LiteralPath (Join-Path $wt 'plugins\core\test-fixtures\pester\SessionLogP2Contracts.Tests.ps1') -Pattern 'Stdout' |
    ForEach-Object { $_.LineNumber.ToString() + ':' + $_.Line.Trim() }
Write-Output '=== SET SESSION TITLE ==='
Select-String -LiteralPath (Join-Path $wt 'plugins\core\lib-ps\repl-invoke.ps1') -Pattern 'function Invoke-WorkflowSetSessionTitle' |
    ForEach-Object { $_.LineNumber }
