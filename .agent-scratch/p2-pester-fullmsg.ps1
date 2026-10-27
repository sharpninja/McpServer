$ErrorActionPreference = 'Stop'
$xmlPath = 'F:\GitHub\McpServer\.worktrees\sessionlife-p2-contracts-5cb2\TestResults\p2-unit-legion-20261002T193045Z\pester\results.xml'
[xml]$xml = Get-Content -LiteralPath $xmlPath
$case = $xml.SelectNodes('//test-case') | Where-Object { $_.name -like '*covers primary queued*' } | Select-Object -First 1
Write-Output ('NAME=' + $case.name)
$msg = $case.SelectSingleNode('failure/message').InnerText
$out = 'F:\GitHub\McpServer\.agent-scratch\p2-pester-fullmsg.txt'
[System.IO.File]::WriteAllText($out, $msg)
Write-Output ('LEN=' + $msg.Length)
Write-Output '=== FIRST 2500 ==='
if ($msg.Length -gt 2500) { $msg.Substring(0, 2500) } else { $msg }
