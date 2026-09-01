#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$src = 'C:\Users\kingd\.grok\sessions\F%3A%5CGitHub%5CMcpServer\01a015ab-72ec-71b3-9dba-c33a67b97e89\terminal\call-40eae4e3-848b-439d-a6a3-867c4e205dfa-91.log'
$dest = 'F:\GitHub\McpServer\docs\receipts\_hv-h5-done-full-test.txt'
Copy-Item -LiteralPath $src -Destination $dest -Force
$info = Get-Item -LiteralPath $dest
Write-Output ('SAVED ' + $dest + ' BYTES=' + $info.Length + ' LWUTC=' + $info.LastWriteTimeUtc.ToString('o'))
$text = Get-Content -LiteralPath $dest -Raw
Write-Output ('HAS_HANDOFF_FAIL=' + ($text -match 'ApproveAsync_LeaseExpiresDuringLiveCreate_SecondInstanceWins'))
Write-Output ('HAS_FAILED_1=' + ($text -match 'Failed:\s+1'))
Write-Output ('HAS_PASSED_1996=' + ($text -match 'Passed:\s+1996'))
Write-Output ('HAS_TEST_FAILED=' + ($text -match 'Test\s+Failed'))
