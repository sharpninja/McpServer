$ErrorActionPreference = 'Continue'
$wt = 'F:\GitHub\McpServer\.worktrees\sessionlife-p2-contracts-5cb2'
Write-Output '=== BEGINTURN STDOUT HITS ==='
Select-String -LiteralPath (Join-Path $wt 'plugins\core\lib-ps\repl-invoke.ps1') -Pattern 'function Invoke-WorkflowBeginTurn|Write-Output|session-verb-outcome|Stdout' |
    Select-Object -First 5 |
    ForEach-Object { $_.LineNumber.ToString() + ':' + $_.Line.Trim() }
Write-Output '=== FUNCTION LINES ==='
Select-String -LiteralPath (Join-Path $wt 'plugins\core\lib-ps\repl-invoke.ps1') -Pattern '^function ' |
    ForEach-Object { $_.LineNumber.ToString() + ':' + $_.Line.Trim() }
Write-Output '=== GROK ERROR ==='
$trx = Join-Path $wt 'TestResults\p2-unit-legion-20261002T193045Z\unit\McpServer.Support.Mcp.Tests\McpServer.Support.Mcp.Tests.trx'
Select-String -LiteralPath $trx -Pattern 'CompleteAsync_Grok_UsesIsolatedCwd' -Context 0,0 |
    Select-Object -First 1 |
    ForEach-Object {
        $line = $_.Line
        $idx = $line.IndexOf('CompleteAsync_Grok')
        if ($idx -lt 0) { $idx = 0 }
        $slice = $line.Substring($idx, [Math]::Min(1800, $line.Length - $idx))
        Write-Output $slice
    }
Write-Output '=== SYNC SCRIPT ==='
Get-ChildItem -LiteralPath $wt -Recurse -Filter '*SyncAgentPlugin*' -File -ErrorAction SilentlyContinue |
    Where-Object { $_.FullName -notmatch '\\(bin|obj|TestResults)\\' } |
    Select-Object -First 15 |
    ForEach-Object { $_.FullName.Replace($wt, '') }
Write-Output '=== CODEX WRAPPER HEAD ==='
Get-Content -LiteralPath 'F:\GitHub\mcpserver-codex-plugin\Invoke-CodexMcpPlugin.ps1' -TotalCount 40
