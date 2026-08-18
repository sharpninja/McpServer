#Requires -Version 7.0
$ErrorActionPreference = 'Stop'

$nonce = [Guid]::NewGuid().ToString('N')
$health = Invoke-WebRequest -Uri ('http://127.0.0.1:7147/health?nonce=' + $nonce) -UseBasicParsing -TimeoutSec 20
Write-Output ('HealthStatus=' + [int]$health.StatusCode)
Write-Output $health.Content
Write-Output ('NonceSent=' + $nonce)

$svc = Get-Service -Name McpServer
Write-Output ('ServiceStatus=' + $svc.Status)
$proc = Get-CimInstance -ClassName Win32_Service -Filter "Name = 'McpServer'"
Write-Output ('ServicePid=' + $proc.ProcessId)
