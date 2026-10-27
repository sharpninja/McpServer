$ErrorActionPreference = 'Continue'
$trx = 'F:\GitHub\McpServer\.worktrees\sessionlife-p2-contracts-5cb2\TestResults\p2-unit-legion-20261002T193045Z\unit\McpServer.Support.Mcp.Tests\McpServer.Support.Mcp.Tests.trx'
Select-String -LiteralPath $trx -Pattern 'outcome="Failed"' |
    Select-Object -First 8 |
    ForEach-Object {
        $line = $_.Line.Trim()
        if ($line.Length -gt 400) { $line = $line.Substring(0, 400) }
        Write-Output $line
    }
Write-Output '=== RESULT SUMMARY SNIP ==='
Select-String -LiteralPath $trx -Pattern 'ResultSummary' | Select-Object -First 3 | ForEach-Object { $_.Line.Trim().Substring(0, [Math]::Min(300, $_.Line.Trim().Length)) }
