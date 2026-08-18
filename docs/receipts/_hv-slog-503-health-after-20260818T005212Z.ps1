#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$nonce = [guid]::NewGuid().ToString('N')
$h = Invoke-WebRequest -Uri ('http://PAYTON-LEGION2:7147/health?nonce=' + $nonce) -UseBasicParsing
$r = Invoke-WebRequest -Uri 'http://PAYTON-LEGION2:7147/ready' -UseBasicParsing
$wmi = Get-CimInstance -ClassName Win32_Service -Filter "Name='McpServer'"
Write-Output ('UTC=' + [datetime]::UtcNow.ToString('o'))
Write-Output ('PID=' + $wmi.ProcessId + ' STATE=' + $wmi.State)
Write-Output ('HEALTH=' + [int]$h.StatusCode + ' ' + $h.Content)
Write-Output ('NONCE=' + $nonce + ' ECHOED=' + $h.Content.Contains($nonce))
Write-Output ('READY=' + [int]$r.StatusCode + ' ' + $r.Content)
