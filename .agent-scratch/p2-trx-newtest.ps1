$ErrorActionPreference = 'Stop'
$trx = 'F:\GitHub\McpServer\.worktrees\sessionlife-p2-contracts-5cb2\TestResults\p2-unit-legion-20261002T222414Z\unit\McpServer.Support.Mcp.Tests\McpServer.Support.Mcp.Tests.trx'
$hits = Select-String -LiteralPath $trx -Pattern 'ReadyMemoryDatabase_DoesNotSkipMissingDatabase|EnsureAgentSessionHeaderColumns_MissingColumns' -SimpleMatch:$false
Write-Output ("HITS=" + @($hits).Count)
$hits | Select-Object -First 8 | ForEach-Object { $_.Line.Substring(0, [Math]::Min(240, $_.Line.Length)) }
