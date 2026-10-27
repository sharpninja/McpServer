$ErrorActionPreference = 'Stop'
$wt = 'F:\GitHub\McpServer\.worktrees\sessionlife-p2-contracts-5cb2'
Write-Output '==== validator methods ===='
Select-String -Path (Join-Path $wt 'tests\McpServer.Support.Mcp.Tests\Services\SessionLogTurnContextValidatorTests.cs') -Pattern 'public (async )?(Task|void) ' |
  ForEach-Object { $_.Line.Trim() }
Write-Output '==== service methods ===='
Select-String -Path (Join-Path $wt 'tests\McpServer.Support.Mcp.Tests\Services\SessionLogServiceTurnContextTests.cs') -Pattern 'public (async )?(Task|void) ' |
  ForEach-Object { $_.Line.Trim() }
Write-Output '==== pester names of interest ===='
[xml]$xml = Get-Content -Raw (Join-Path $wt 'TestResults\p2-unit-legion-20261002T202158Z\pester\results.xml')
$cases = $xml.SelectNodes('//test-case')
foreach ($c in $cases) {
  $name = [string]$c.name
  if ($name -match 'failsafe|AppendDialog|appendDialog|not_found|exact None|queued|degraded|auditDialog|SubmitAsync|NoneWhen|replay') {
    Write-Output ($c.result + ' | ' + $name)
  }
}
